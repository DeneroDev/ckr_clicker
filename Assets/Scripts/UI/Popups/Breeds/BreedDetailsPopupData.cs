namespace UI.Popups.Breeds
{
    public readonly struct BreedDetailsPopupData
    {
        public BreedDetailsPopupData(string title, string description)
        {
            Title = title;
            Description = description;
        }

        public string Title { get; }
        public string Description { get; }
    }
}
