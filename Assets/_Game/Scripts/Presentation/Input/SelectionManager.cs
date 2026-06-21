using UnityEngine;
using SpringAutumn.Bootstrap;

namespace SpringAutumn.Presentation.Input
{
    public class SelectionManager : MonoBehaviour
    {
        public ISelectable Current { get; private set; }
        public GameApplication Application { get; private set; }

        public void Bind(GameApplication application)
        {
            Application = application;
        }

        public void Select(ISelectable selectable)
        {
            if (Current == selectable)
                return;

            Current?.OnDeselected();
            Current = selectable;
            Current?.OnSelected();

            if (Application != null && Current != null)
            {
                Application.Events.Publish(new SelectionChanged
                {
                    Id = Current.Id,
                    Type = Current.Type
                });
            }
        }

        public void Clear()
        {
            Current?.OnDeselected();
            Current = null;
        }
    }
}
