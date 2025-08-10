namespace RoboClerk
{
    public class EliminatedRequirementItem : EliminatedLinkedItem
    {

        public EliminatedRequirementItem() 
        {
            //for serialization
        }

        public EliminatedRequirementItem(RequirementItem originalItem, string reason, EliminationReason eliminationType)
            : base(originalItem, reason, eliminationType)
        {
            // For requirements, we need to keep their specific properties
            RequirementType = originalItem.TypeOfRequirement;
            RequirementState = originalItem.RequirementState;
            RequirementDescription = originalItem.RequirementDescription;
            RequirementAssignee = originalItem.RequirementAssignee;
        }

        public RequirementType RequirementType { get; private set; }
        public string RequirementState { get; private set; } = string.Empty;
        public string RequirementDescription { get; private set; } = string.Empty;
        public string RequirementAssignee { get; private set; } = string.Empty;
    }
}
