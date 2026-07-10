using EG.Core.Common;

namespace GameBrain.SDK.Monetization.Util
{
    public class MethodInvoker : Singleton<MethodInvoker>
    {
        private void OnDisable()
        {
            _current.CancelInvoke();
        }
    }
}
