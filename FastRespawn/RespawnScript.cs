using ThunderRoad;

namespace FastRespawn
{
    public class RespawnScript : ThunderScript
    {
        // Source: @Wully on BaS Discord
        // Big help in getting this ready for U12
        public static ModOptionFloat[] zeroToOneHundered()
        {
            ModOptionFloat[] options = new ModOptionFloat[101];
            float val = 0;
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionFloat(val.ToString("0.0"), val);
                val += 1f;
            }
            return options;
        }

        private static LevelModuleDeath.Behaviour deathBehaviour;
        private static float delay;

        [ModOption(name: "Death Behaviour", tooltip: "Determines which type of death behaviour you want.", defaultValueIndex = 1, order = 0)]
        public static void DeathBehaviour(LevelModuleDeath.Behaviour behaviour)
        {
            try
            {
                deathBehaviour = behaviour;

                if (Level.current.mode.TryGetModule<LevelModuleDeath>(out var deathModule))
                    deathModule.behaviour = behaviour;
            }
            catch { }
        }

        [ModOption(name: "Delay Before Load", tooltip: "Determines how long to wait in seconds before triggering the death behaviour.", valueSourceName = nameof(zeroToOneHundered), defaultValueIndex = 3, order = 1)]
        public static void Delay(float delayBeforeLoad)
        {
            try
            {
                delay = delayBeforeLoad;

                if (Level.current.mode.TryGetModule<LevelModuleDeath>(out var deathModule))
                    deathModule.delayBeforeLoad = delay;
            }
            catch { }
        }

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            EventManager.onLevelLoad += OnLevelLoad;
        }

        private void OnLevelLoad(LevelData levelData, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd && Level.current.mode.TryGetModule<LevelModuleDeath>(out var deathModule))
            {
                deathModule.behaviour = deathBehaviour;
                deathModule.delayBeforeLoad = delay;
            }
        }
    }
}
