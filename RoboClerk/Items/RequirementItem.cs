using System;

namespace RoboClerk
{
    public enum RequirementType
    {
        SystemRequirement,
        SoftwareRequirement
    };
    public class RequirementItem : LinkedItem
    {
        private RequirementType requirementType;
        private string requirementState = string.Empty;
        private string requirementTitle = string.Empty;
        private string requirementDescription = string.Empty;
        private string requirementRevision = string.Empty;
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

        public string RequirementTitle
        {
            get => requirementTitle;
            set => requirementTitle = value;
        }

        public string RequirementDescription
        {
            get => requirementDescription;
            set => requirementDescription = value;
        }

        public string RequirementRevision
        {
            get => requirementRevision;
            set => requirementRevision = value;
        }
    }
}

