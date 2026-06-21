namespace SpringAutumn.Presentation.Input
{
    public interface ISelectable
    {
        string Id { get; }
        SelectionType Type { get; }
        void OnSelected();
        void OnDeselected();
    }
}
