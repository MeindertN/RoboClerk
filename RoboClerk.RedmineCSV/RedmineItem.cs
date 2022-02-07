namespace RoboClerk.RedmineCSV
{
    public class NameAttribute : System.Attribute
    {
        private string name;

        public NameAttribute(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get => name;
        }
    }

    class RedmineItem
    {
        [Name("#")]
        public string Id { get; set; }

        [Name("Project")]
        public string Project { get; set; }

        [Name("Tracker")]
        public string Tracker { get; set; }

        [Name("Parent task")]
        public string ParentTask { get; set; }

        [Name("Parent task subject")]
        public string ParentTaskSubject { get; set; }

        [Name("Status")]
        public string Status { get; set; }

        [Name("Priority")]
        public string Priority { get; set; }

        [Name("Subject")]
        public string Subject { get; set; }

        [Name("Author")]
        public string Author { get; set; }

        [Name("Assignee")]
        public string Assignee { get; set; }

        [Name("Updated")]
        public string Updated { get; set; }

        [Name("Category")]
        public string Category { get; set; }

        [Name("Target version")]
        public string TargetVersion { get; set; }

        [Name("Start date")]
        public string StartDate { get; set; }

        [Name("Due date")]
        public string DueDate { get; set; }

        [Name("Estimated time")]
        public string EstimatedTime { get; set; }

        [Name("Total estimated time")]
        public string TotalEstimatedTime { get; set; }

        [Name("Spent time")]
        public string SpentTime { get; set; }

        [Name("Total spent time")]
        public string TotalSpentTime { get; set; }

        [Name("% Done")]
        public string PercentDone { get; set; }

        [Name("Created")]
        public string Created { get; set; }

        [Name("Closed")]
        public string Closed { get; set; }

        [Name("Last updated by")]
        public string LastUpdatedBy { get; set; }

        [Name("Related issues")]
        public string RelatedIssues { get; set; }

        [Name("Files")]
        public string Files { get; set; }

        [Name("Tags")]
        public string Tags { get; set; }

        [Name("Checklist")]
        public string Checklist { get; set; }

        [Name("Functional Area")]
        public string FunctionalArea { get; set; }

        [Name("Private")]
        public string Private { get; set; }

        [Name("Story points")]
        public string StoryPoints { get; set; }

        [Name("Sprint")]
        public string Sprint { get; set; }

        [Name("Description")]
        public string Description { get; set; }

        [Name("Last notes")]
        public string LastNotes { get; set; }

        [Name("Test Method")]
        public string TestMethod { get; set; }
    }
}
