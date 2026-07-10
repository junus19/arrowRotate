using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GameBrain.Casual
{
    public class Level : ILevel
    {
        protected readonly LevelData _data;
        protected readonly LevelStats _stats;
        protected readonly Transform _container;
        protected Status _status = Status.NotCompleted;
        
        public LevelData Data => _data;
        public LevelStats Stats => _stats;
        public Transform Container => _container;
        public Status Status => _status;
        public Scene Scene;
        
        public event Action OnStart;
        public event Action<Status> OnFinish;
        public event Action OnRestart;
        
        public Level(LevelData data, LevelStats stats, Transform container = null)
        {
            _data = data;
            _stats = stats;
            _container = container;
        }

        public async Awaitable Load()
        {
            if (!string.IsNullOrEmpty(_data.SceneName) && !string.IsNullOrWhiteSpace(_data.SceneName) && _data.SceneName.ToLower().Trim() != "none")
            {
                await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync(_data.SceneName, LoadSceneMode.Additive));
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(_data.SceneName));
            }
            Scene = new Scene();
            OnLoaded();
        }

        public async Awaitable Unload()
        {
            if (!string.IsNullOrEmpty(_data.SceneName) && !string.IsNullOrWhiteSpace(_data.SceneName) && _data.SceneName.ToLower().Trim() != "none")
                await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(_data.SceneName));
            OnUnloaded();
            if (_container != null)
                Object.Destroy(_container.gameObject);
        }

        public void Start()
        {
            _data.Objectives.ForEach(objective => objective.Reset());
            _data.Objectives.ForEach(objective => objective.OnFailed += OnObjectiveFailed);
            _data.Objectives.ForEach(objective => objective.OnAchieved += OnObjectiveAchieved);
            OnStarted();
            OnStart?.Invoke();
        }

        public void Finish(Status status)
        {
            _data.Objectives.ForEach(objective => objective.OnFailed -= OnObjectiveFailed);
            _data.Objectives.ForEach(objective => objective.OnAchieved -= OnObjectiveAchieved);
            _status = status;
            
            switch (status)
            {
                case Status.Success:
                    OnSuccess();
                    break;
                case Status.Fail:
                    OnFailure();
                    break;
                case Status.NotCompleted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
            OnFinish?.Invoke(status);
        }

        public void Restart()
        {
            _data.Objectives.ForEach(objective => objective.Reset()); // Reset the objectives!
            _stats.Reset(); // Reset the stats!
            _status = Status.NotCompleted;

            OnRestarted();
            OnRestart?.Invoke();
        }

        private void OnObjectiveFailed()
        {
            _status = Status.Fail;
            Finish(_status);
        }

        private void OnObjectiveAchieved()
        {
            if (_data.Objectives.Any(objective => objective.Status == Status.NotCompleted))
                return;
            _status = Status.Success;
            Finish(_status);
        }

        protected virtual void OnLoaded()
        {
        }

        protected virtual void OnUnloaded()
        {
        }

        protected virtual void OnStarted()
        {
        }

        protected virtual void OnSuccess()
        {
        }

        protected virtual void OnFailure()
        {
        }

        protected virtual void OnRestarted()
        {
        }
    }
}
