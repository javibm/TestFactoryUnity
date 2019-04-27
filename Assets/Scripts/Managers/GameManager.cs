﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LD44
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game Config")]
        [SerializeField]
        private int initialChamacos;

        [SerializeField]
        private int timePerDay;

        [SerializeField]
        private float chamacoEnergyPerSecond;

        [SerializeField]
        private int chamacoSecondsWorking;

        [SerializeField]
        private int chamacosKilledPerFail;

        [SerializeField]
        private int chamacoSecondsResting;

        [SerializeField]
        private int alienEnergyRequest;

        [SerializeField]
        private int sacrificeCost;

        [SerializeField]
        private int drugsPerSacrifice;

        [Header("UI")]
        [SerializeField]
        private GameplayUIManager gameplayUIManager;

        private int days;
        public int Days
        {
            get
            {
                return days;
            }
        }

        private int ticks;

        private int drugs;

        private int currentChamacos;

        private int readyChamacos;

        private int restingChamacos;

        private int workingChamacos;
        public int WorkingChamacos
        {
            get
            {
                return workingChamacos;
            }
        }

        public Action OnGameOver;
        public Action<int> OnCurrentChamacosModified;
        public Action<int> OnWorkingChamacosModified;
        public Action<int> OnRestingChamacosModified;
        public Action<int> OnReadyChamacosModified;
        public Action<int> OnDrugsModified;

        public void Awake()
        {
            InitGame();
        }
        public void InitGame()
        {
            gameplayUIManager.Init();

            // Reset
            SetDays(0);
            SetDrugs(0);
            NewChamacos(initialChamacos);
            SetRestingChamacos(0);
            SetWorkingChamacos(0);

            // Arranca el TimeManager
            TimeManager.Instance.Init(timePerDay);
            TimeManager.Instance.OnDayEnded += OnDayEnded;

            // FactoryManager
            FactoryManager.Instance.Init(chamacoEnergyPerSecond);
            AlienManager.Instance.Init(alienEnergyRequest, sacrificeCost);
            // Notificar a marcos que instancia los chamacos en Ready

        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayEnded -= OnDayEnded;
            }
        }

        private void SendChamacoToRest()
        {
            // Solo se pueden mover chamacos a rest desde work
            SetWorkingChamacos(--workingChamacos);
            SetRestingChamacos(++restingChamacos);

            // Notificar a Marcos para que los mueva

            // Timer para que dejen de descansar
            TimeManager.Instance.SetTimer(chamacoSecondsResting, SendChamacoToReady);
        }

        public void SendChamacoToWork()
        {
            if (readyChamacos > 0)
            {
                // Solo se pueden mover desde ready
                SetWorkingChamacos(++workingChamacos);
                SetReadyChamacos(--readyChamacos);

                // Notificar a Marcos para que los mueva

                // Timer para que dejen de trabajar
                TimeManager.Instance.SetTimer(chamacoSecondsWorking, SendChamacoToRest);
            }
        }

        private void SendChamacoToReady()
        {
            if (restingChamacos > 0)
            {
                // Se pueden llamar a ready poque ha pasado el tiempo de rest o porque has gastado comida
                SetRestingChamacos(--restingChamacos);
                SetReadyChamacos(++readyChamacos);

                // Notificar a Marcos para que los mueva
            }
        }

        public void SacrificeChamacos()
        {
            if (currentChamacos > sacrificeCost)
            {
                KillChamacos(sacrificeCost);
                SetDrugs(drugs + drugsPerSacrifice);
            }
        }

        public void UseDrugs()
        {
            if (drugs > 0 && restingChamacos > 0)
            {
                SetReadyChamacos(++readyChamacos);
                SetRestingChamacos(--restingChamacos);
                SetDrugs(--drugs);
            }
        }

        private void NewChamacos(int quantity)
        {
            SetReadyChamacos(readyChamacos + quantity);
            SetCurrentChamacos(currentChamacos + quantity);
            // Notificar a Marcos para que los spawnee

        }

        private void KillChamacos(int quantity)
        {
            SetReadyChamacos(readyChamacos - quantity);
            SetCurrentChamacos(currentChamacos - quantity);
            // Notificar a Marcos para que los despawnee

            CheckChumacosGameOver();
        }

        private void SetDrugs(int value)
        {
            drugs = value;
            if (OnDrugsModified != null)
            {
                OnDrugsModified(value);
            }
        }

        private void SetDays(int value)
        {
            days = value;
            gameplayUIManager.UpdateDaysLabel(value);
        }

        private void SetCurrentChamacos(int value)
        {
            currentChamacos = value;
            if (OnCurrentChamacosModified != null)
            {
                OnCurrentChamacosModified(value);
            }
        }

        private void SetWorkingChamacos(int value)
        {
            workingChamacos = value;
            if (OnWorkingChamacosModified != null)
            {
                OnWorkingChamacosModified(value);
            }
        }

        private void SetRestingChamacos(int value)
        {
            restingChamacos = value;
            if (OnRestingChamacosModified != null)
            {
                OnRestingChamacosModified(value);
            }
        }

        private void SetReadyChamacos(int value)
        {
            readyChamacos = value;
            if (OnReadyChamacosModified != null)
            {
                OnReadyChamacosModified(value);
            }
        }

        private void CheckChumacosGameOver()
        {
            if (readyChamacos <= 0)
            {
                OnGameOver();
            }
        }

        private void OnDayEnded()
        {
            SetDays(++days);

            if (FactoryManager.Instance.CurrentEnergy < AlienManager.Instance.CurrentEnergyNeeded)
            {
                KillChamacos(chamacosKilledPerFail);
            }
            else
            {
                int energyForShip = FactoryManager.Instance.CurrentEnergy - AlienManager.Instance.CurrentEnergyNeeded;
                ShipManager.Instance.AddEnergy(energyForShip);
            }

            FactoryManager.Instance.ResetEnergy();
        }
    }
}
