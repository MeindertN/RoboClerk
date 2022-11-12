using System;

namespace RoboClerk
{
    public enum RequirementType
    {
        SystemRequirement,
        SoftwareRequirement,
        DocumentationRequirement
    };
    public class RequirementItem : LinkedItem
    {
        private RequirementType requirementType;
        private string requirementState = string.Empty;
        private string requirementDescription = string.Empty;
        private string requirementAssignee = string.Empty;
        public RequirementItem(RequirementType typeOfRequirement)
        {
            TypeOfRequirement = typeOfRequirement;
            id = Guid.NewGuid().ToString();
        }

        public RequirementType TypeOfRequirement
        {
            get => requirementType;
            set
            {
                if(value == RequirementType.SystemRequirement)
                {
                    type = "SystemRequirement";
                }
                else
                {
                    type = "SoftwareRequirement";
                }
                requirementType = value;
            }
        }

        public string RequirementState
        {
            get => requirementState;
            set => requirementState = value;
        }

        public string RequirementAssignee
        {
            get => requirementAssignee;
            set => requirementAssignee = value;
        }

        public string RequirementDescription
        {
            get => requirementDescription;
            set => requirementDescription = value;
        }
    }
}

