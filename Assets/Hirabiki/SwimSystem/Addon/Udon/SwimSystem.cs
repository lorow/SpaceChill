namespace Hirabiki.Udon.Works
{
    using UdonSharp;
    using UnityEngine;
    using UnityEngine.UI;
    using VRC.SDKBase;
    using VRC.Udon;

    public class SwimSystem : UdonSharpBehaviour
    {
        /// <summary>
        /// This UdonBehaviour is only responsible for abstract simulations
        /// The only allowed physics movement is the sinking force
        /// </summary>
        [Tooltip("Script updates every this many ticks (1/90)")]
        [SerializeField] private int updateInterval = 4;

        [Header("Required configurations")]
        [Tooltip("Assign to Udon_SwimLocomotion in SwimSystem prefab")]
        [SerializeField] private UdonBehaviour swimLocomotion;

        [Tooltip("Assign to BreathSystemEffect prefab")]
        [SerializeField] private UdonBehaviour localPlayerEffect;

        [Tooltip("Assign a respawn location after drowning")]
        [SerializeField] private Transform respawnPosition;

        [Tooltip("If using combat system, assign to empty GameObject")]
        [SerializeField] private GameObject combatDamagePrefab;

        private int updateDelay;
        private int startDelay = 2;
        private bool queueUnderwaterDuringStart;

        private bool isSwimming;
        private bool isUnderwater;

        [Header("General settings")]
        [Tooltip("Can player drown?\nYou may also delete BreathSystem and BreathSystemEffect prefabs from the scene to remove drowning and gain a bit of performance")]
        [SerializeField] private bool canDrown = true;

        [Tooltip("If enabled, avatar ragdolls after drowning")]
        public bool useCombatSystem;

        [Tooltip("Breath consumption when moving underwater\n= 1 + SwimSpeed * x")]
        [SerializeField] private float movementIkiUsage = 0.5f;

        [Tooltip("Breath recovery rate (seconds/second)")]
        [SerializeField] private float ikiRegenRate = 10f;

        [Tooltip("Maximum health")]
        [SerializeField] private float maxCombatHP = 3f;

        [Tooltip("Health recovery rate\nHealth drains after having no air after drowning")]
        [SerializeField] private float combatHPRegenRate = 0.06f;

        [Tooltip("Maximum air capacity (mL)")]
        [SerializeField] private float maxKuuki = 2500f;

        [Tooltip("Maximum breath-holding time (seconds)")]
        [SerializeField] private float maxIki = 150f;

        //[Tooltip("Negative buoyancy without air in lung")]
        //[SerializeField] private float sinkingGravity = 0.05f;

        private float virtualCombatHP;
        private float simKuukiLeakTime;
        private float simKuuki;
        private float simIki;

        [Header("Sound settings")]
        [Tooltip("AudioSource for sound effects")]
        [SerializeField] private AudioSource localOneShotSounds;

        [Tooltip("Sound effect when fully recovered breath")]
        [SerializeField] private AudioClip fullRecoveryClip;

        [Header("Debug text")]
        [SerializeField] private Text debugText;
        [SerializeField] private Text debugText2;
        private UdonBehaviour globalPlayerEffect;
        private bool objectAssigned;
        private bool queueRespawn;

        private bool queueFullRecoverySound;

        void Start()
        {
            simKuuki = maxKuuki;
            simIki = maxIki;

            globalPlayerEffect = localPlayerEffect;

            if(useCombatSystem && Networking.LocalPlayer != null)
            {
                Networking.LocalPlayer.CombatSetup(); // MAKES OnTrigger#### with local player UNRELIABLE!!
                Networking.LocalPlayer.CombatSetMaxHitpoints(maxCombatHP);
                Networking.LocalPlayer.CombatSetRespawn(true, 5f, respawnPosition);
                Networking.LocalPlayer.CombatSetDamageGraphic(combatDamagePrefab);
            } else
            {
                virtualCombatHP = maxCombatHP;
            }

            WhenCombatRespawn();
        }

        void LateUpdate()
        {
            if(Networking.LocalPlayer == null) return;
            if(queueRespawn)
            {
                if(respawnPosition == null)
                {
                    Networking.LocalPlayer.TeleportTo(new Vector3(0f, -1048576f, 0f), Networking.LocalPlayer.GetRotation());
                } else
                {
                    Networking.LocalPlayer.TeleportTo(respawnPosition.position, respawnPosition.rotation);
                }
                queueRespawn = false;
            }

            if(startDelay > 0)
            {
                startDelay -= 1;
            } else if(queueUnderwaterDuringStart)
            {
                queueUnderwaterDuringStart = false;
                OnUnderwaterEnter();
            }
        }

        void FixedUpdate()
        {
            if(Networking.LocalPlayer == null) return;
            // Every tick
            if(--updateDelay > 0) return;
            // Every nth ticks
            updateDelay = updateInterval;
            float fixedTimeStep = Time.fixedDeltaTime * updateInterval;

            // Not for use as locomotion, but energy consumption
            // Locomotion is controlled within SwimLocomotion (but swimEnergy can be set to do weakening)
            float swimEnergy = (float)swimLocomotion.GetProgramVariable("swimEnergy");
            swimEnergy = simIki < 0f ? Mathf.Lerp(0.0f, Mathf.Lerp(0.666666666f, 1f, KuukiRatio()), CombatHPRatio()) : 1f;
            swimLocomotion.SetProgramVariable("swimEnergy", swimEnergy);

            Vector3 inputVector = (Vector3)swimLocomotion.GetProgramVariable("swimVectorEcho") * swimEnergy;

            // TODO: Apply sinking force to locomotion
            float buoyancyRatio = (float)swimLocomotion.GetProgramVariable("buoyancyRatio");
            buoyancyRatio = Mathf.Lerp(0.25f, 1.00f, KuukiRatio());
            swimLocomotion.SetProgramVariable("buoyancyRatio", buoyancyRatio);

            // Abstract simulation numbers begins here!
            float lastIki = simIki;
            float lastKuuki = simKuuki;
            float ikiDelta = -inputVector.magnitude * movementIkiUsage - 1f;
            if(!isUnderwater)
            {
                ikiDelta += ikiRegenRate;
            }
            if(canDrown)
            {
                simIki += ikiDelta * fixedTimeStep;
            }

            if(isUnderwater)
            {
                if(IkiRatio() < 0.8f)
                {
                    queueFullRecoverySound = true;
                }
                if(simIki <= 0f)
                {
                    // When simIki is already negative, this event wouldn't fire. This case is covered by WhenUnderwater()
                    if(lastIki > 0f) // <-- This is why
                    {
                        TrySendCustomNetworkEvent("SetExhaleTrue");
                    }
                    simKuuki -= 350f * fixedTimeStep;
                } else
                {
                    simKuukiLeakTime += ikiDelta * fixedTimeStep;
                    if(simKuukiLeakTime <= 0f)
                    {
                        ExhaleSmall();
                    }
                    SetHitpoints(GetHitpoints() + IkiRatio() * combatHPRegenRate * fixedTimeStep);
                }

                if(simKuuki <= 0f)
                {
                    if(lastKuuki > 0f)
                    {
                        TrySendCustomNetworkEvent("SetExhaleFalse");
                    }
                    simKuuki = 0f;
                    SetHitpoints(GetHitpoints() + IkiRatioUnclamped() * fixedTimeStep);
                }

                if(!useCombatSystem && CombatHPRatio() == 0f)
                {
                    queueRespawn = true;
                    WhenCombatRespawn();
                }
            } else
            {
                SetHitpoints(GetHitpoints() + combatHPRegenRate * fixedTimeStep);

                if(queueFullRecoverySound && simIki >= maxIki)
                {
                    localOneShotSounds.PlayOneShot(fullRecoveryClip, localOneShotSounds.volume);
                    queueFullRecoverySound = false;
                }

                simIki = Mathf.Clamp(simIki, maxIki * -0.05f, maxIki);

                simKuuki += 1500f * fixedTimeStep;
                simKuuki = simKuuki > maxKuuki ? maxKuuki : simKuuki;
            }

            DebugOutput();
        }

        public void SetGlobalEffectObject(UdonBehaviour geo)
        {
            if(geo != null)
            {
                DebugOutputLog("[Local] GlobalPlayerEffect assigned");
                globalPlayerEffect = geo;
                objectAssigned = true;
            } else
            {
                DebugOutputLog("[Local] ERROR: Assign param is null!");
            }
        }

        private void TrySendCustomNetworkEvent(string eventName)
        {
            if(objectAssigned)
            {
                globalPlayerEffect.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, eventName);
            } else
            {
                globalPlayerEffect.SendCustomEvent(eventName);
            }
        }

        public void WhenCombatRespawn()
        {
            simKuuki = maxKuuki;
            simIki = maxIki;
        }

        private float GetHitpoints()
        {
            return useCombatSystem ? Networking.LocalPlayer.CombatGetCurrentHitpoints() : virtualCombatHP;
        }
        private void SetHitpoints(float hp)
        {
            if(useCombatSystem)
            {
                Networking.LocalPlayer.CombatSetCurrentHitpoints(hp);
            } else
            {
                virtualCombatHP = Mathf.Clamp(hp, 0f, maxCombatHP);
            }
        }

        public float IkiRatio() { return Mathf.Clamp01(simIki / maxIki); }
        public float KuukiRatio() { return Mathf.Clamp01(simKuuki / maxKuuki); }
        public float CombatHPRatio() { return GetHitpoints() / maxCombatHP; }

        public float IkiRatioUnclamped() { return simIki / maxIki; }
        public float KuukiRatioUnclamped() { return simKuuki / maxKuuki; }

        public float GetIki() { return simIki; }

        public bool IsSwimming() { return isSwimming; }
        public bool IsUnderwater() { return isUnderwater; }

        // Public methods normally used by Udon_SwimLocomotion (SendCustomEvent)
        public void OnSwimEnter()
        {
            isSwimming = true;
        }
        public void OnSwimExit()
        {
            isSwimming = false;
        }
        public void OnUnderwaterEnter()
        {
            if(startDelay > 0)
            {
                queueUnderwaterDuringStart = true;
                return;
            }

            isUnderwater = true;
            if(simIki <= 0f)
            {
                TrySendCustomNetworkEvent("SetExhaleTrue");
            }

            simKuukiLeakTime = 10f * IkiRatio() * Mathf.Pow(2f, Random.Range(-0.25f, 0.25f));
            if(globalPlayerEffect != null)
            {
                globalPlayerEffect.SendCustomEvent("SyncMouthTransform");
            } else
            {
                localPlayerEffect.SendCustomEvent("SyncMouthTransform");
            }
            if(globalPlayerEffect == localPlayerEffect)
            {
                DebugOutputLog("[Local] WARN: Using local object (fail-safe)");
            }
        }
        public void OnUnderwaterExit()
        {
            isUnderwater = false;
            TrySendCustomNetworkEvent("SetExhaleFalse");
        }
        public void MuteAllSounds()
        {
            localOneShotSounds.mute = true;
            TrySendCustomNetworkEvent("MuteAllSounds");
        }
        public void UnmuteAllSounds()
        {
            localOneShotSounds.mute = false;
            TrySendCustomNetworkEvent("UnmuteAllSounds");
        }

        private void ExhaleSmall()
        {
            TrySendCustomNetworkEvent("BurstSmall");

            simKuuki -= 40f;
            float halfSmooth = Mathf.Lerp(Mathf.SmoothStep(0f, 1f, IkiRatio()), IkiRatio(), 0.75f);
            simKuukiLeakTime = (1.5f * Mathf.Sqrt(maxIki) * halfSmooth + 0.5f) * Mathf.Pow(2f, Random.Range(-0.5f, 0.5f));
        }

        private void DebugOutput()
        {
            if(debugText == null) return;

            debugText.text = string.Format("ikitsugi deki{0}\niki: {1}\nkuuki: {2}\nhakidasu time: {3}\nCombatSystem HP: {4}",
                isUnderwater ? "nai" : "ru", simIki, simKuuki, simKuukiLeakTime, GetHitpoints());
        }
        private void DebugOutputLog(string text)
        {
            if(debugText2 == null) return;

            debugText2.text = text + "\n" + debugText2.text;
        }
    }
}