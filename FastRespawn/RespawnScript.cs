using HarmonyLib;
using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace FastRespawn
{
    public class RespawnScript : ThunderScript
    {
        public static ModOptionFloat[] ZeroToTenInTenths()
        {
            ModOptionFloat[] options = new ModOptionFloat[101];
            float val = 0;
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionFloat(val.ToString("0.0"), val);
                val += 0.1f;
            }
            return options;
        }

        private static LevelData.Mode.PlayerDeathAction _deathBehavior;
        private static float _deathSlowTimeDuration;
        private static float _deathNormalTimeDuration;
        private static float _deathFadeTimeDuration;

        [ModOption(
            name: "Death Behaviour", 
            tooltip: "Determines which type of death behaviour you want.", 
            defaultValueIndex = 1, 
            order = 0)]
        public static void DeathBehaviour(LevelData.Mode.PlayerDeathAction action)
        {
            _deathBehavior = action;
        }

        [ModOption(
            name: "Slow Time Duration", 
            tooltip: "Determines how long to be in slow time after you die in seconds.", 
            valueSourceName = nameof(ZeroToTenInTenths), 
            defaultValueIndex = 3, 
            order = 1)]
        public static void SlowTimeDuration(float slowTimeDuration)
        {
            _deathSlowTimeDuration = slowTimeDuration;
        }

        [ModOption(
            name: "Normal Time Duration",
            tooltip: "Determines how long to be in normal time after you die.",
            valueSourceName = nameof(ZeroToTenInTenths),
            defaultValueIndex = 0,
            order = 2)]
        public static void NormalTimeDuration(float normalTimeDuration)
        {
            _deathNormalTimeDuration = normalTimeDuration;
        }

        [ModOption(
            name: "Fade Time Duration",
            tooltip: "Determines how long it takes for the screen to fade after you die.",
            valueSourceName = nameof(ZeroToTenInTenths),
            defaultValueIndex = 0,
            order = 3)]
        public static void FadeTimeDuration(float fadeTimeDuration)
        {
            _deathFadeTimeDuration = fadeTimeDuration;
        }

        [ModOption(
            name: "Show Message On Death",
            tooltip: "Determines whether to show the death message on player death.",
            defaultValueIndex = 0,
            order = 4)]
        public static bool showMessageOnDeath;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            new Harmony("DeathSequenceCoroutine").PatchAll();
        }

        [HarmonyPatch(typeof(Level), "DeathSequenceCoroutine")]
        class DeathSequencePatch
        {
            public static bool Prefix( 
                ref bool askReload,
                ref float slowTimeduration, 
                ref float normalTimeduration, 
                ref float endingFadeDuration, 
                ref System.Action endCallback)
            {
                slowTimeduration = _deathSlowTimeDuration * Catalog.gameData.deathSlowMoRatio;
                normalTimeduration = _deathNormalTimeDuration;
                endingFadeDuration = _deathFadeTimeDuration;

                switch (_deathBehavior)
                {
                    case LevelData.Mode.PlayerDeathAction.AskReload:
                        askReload = true;
                        break;
                    case LevelData.Mode.PlayerDeathAction.LoadHome:
                        askReload = false;
                        endCallback = () => LevelManager.LoadLevel(Player.characterData.mode.data.levelHome, Player.characterData.mode.data.levelHomeModeName);
                        break;
                    case LevelData.Mode.PlayerDeathAction.PermaDeath:
                        askReload = false;
                        endCallback = () => LevelManager.LoadLevel(ThunderRoadSettings.current.game.mainMenuLevelId);
                        break;
                    default:
                        askReload = false;
                        endCallback = () => LevelManager.ReloadLevel();
                        break;
                }

                if (!askReload && !showMessageOnDeath)
                {
                    GameManager.local.StartCoroutine(NoMessageDeathCoroutine(endCallback));
                    return false;
                }

                return true;
            }

            // This is literally just Level.DeathSequenceCoroutine without the message posting
            private static IEnumerator NoMessageDeathCoroutine(System.Action endCallback)
            {
                if (UIPlayerMenu.instance != null)
                {
                    UIPlayerMenu.instance.IsOpeningBlocked = true;
                    UIPlayerMenu.instance.Close();
                }
                if (Catalog.gameData.useDynamicMusic && ThunderBehaviourSingleton<MusicManager>.HasInstance)
                    ThunderBehaviourSingleton<MusicManager>.Instance.Volume = 1f;
                WaveSpawner waveSpawner;
                if (WaveSpawner.TryGetRunningInstance(out waveSpawner))
                    waveSpawner.CancelWave();
                Player.local.locomotion.enabled = false;
                EffectData outputData;
                if (Catalog.TryGetData(Catalog.gameData.deathEffectId, out outputData))
                    TimeManager.SetSlowMotion(true, Catalog.gameData.deathSlowMoRatio, Catalog.gameData.deathSlowMoEnterCurve, outputData);
                else
                    TimeManager.SetSlowMotion(true, Catalog.gameData.deathSlowMoRatio, Catalog.gameData.deathSlowMoEnterCurve);
                CameraEffects.SetSepia(Level.current, 1f);
                yield return new WaitForSeconds(_deathSlowTimeDuration * Catalog.gameData.deathSlowMoRatio);
                TimeManager.SetSlowMotion(false, Catalog.gameData.deathSlowMoRatio, Catalog.gameData.deathSlowMoExitCurve);
                yield return new WaitForSeconds(_deathNormalTimeDuration);
                CameraEffects.DoFadeEffect(true, _deathFadeTimeDuration);
                yield return new WaitForSeconds(_deathFadeTimeDuration);
                if (endCallback != null)
                    endCallback();
            }
        }
    }
}
