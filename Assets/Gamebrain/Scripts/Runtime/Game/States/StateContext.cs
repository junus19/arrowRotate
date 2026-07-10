using EC.Core.Common;

namespace GameBrain.Casual
{
    public abstract class StateContext
    {
        protected readonly StateMachine _stateMachine;

        public StateMachine StateMachine => _stateMachine;

        protected StateContext(StateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }
    }
}
