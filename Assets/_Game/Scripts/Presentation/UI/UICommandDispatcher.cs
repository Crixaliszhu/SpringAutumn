using UnityEngine;
using SpringAutumn.Bootstrap;
using SpringAutumn.Commands;

namespace SpringAutumn.Presentation.UI
{
    public class UICommandDispatcher : MonoBehaviour
    {
        public GameApplication Application { get; private set; }

        public void Bind(GameApplication application)
        {
            Application = application;
        }

        public bool Enqueue(GameCommand command)
        {
            if (Application?.Engine == null || command == null)
                return false;

            Application.Engine.EnqueueCommand(command);
            return true;
        }
    }
}
