using System.Collections.Generic;

namespace RoboClerk
{
    public class CheckpointDataStorage
    {
        private List<RequirementItem> systemRequirements = new List<RequirementItem>();
        private List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        private List<RiskItem> risks = new List<RiskItem>();
        private List<SOUPItem> soups = new List<SOUPItem>();
        private List<TestCaseItem> softwareSystemTests = new List<TestCaseItem>();
        private List<UnitTestItem> unitTests = new List<UnitTestItem>();
        private List<AnomalyItem> anomalies = new List<AnomalyItem>();

        public List<RequirementItem> SystemRequirements 
        { 
            get
            {
                return systemRequirements;
            }
            set
            {
                systemRequirements = value;
            }
        }

        public List<RequirementItem> SoftwareRequirements 
        { 
            get
            {
                return softwareRequirements;
            }
            set
            {
                softwareRequirements = value;
            }
        }

        public List<RiskItem> Risks
        {
            get
            {
                return risks;
            }
            set
            {
                risks = value;
            }
        }


        public List<SOUPItem> SOUPs
        {
            get
            {
                return soups;
            }
            set
            {
                soups = value;
            }
        }

        public List<TestCaseItem> SoftwareSystemTests
        {
            get
            {
                return softwareSystemTests;
            }
            set
            {
                softwareSystemTests = value;
            }
        }

        public List<UnitTestItem> UnitTests
        {
            get
            {
                return unitTests;
            }
            set
            {
                unitTests = value;
            }
        }

        public List<AnomalyItem> Anomalies
        {
            get
            {
                return anomalies;
            }
            set
            {
                anomalies = value;
            }
        }

        public void UpdateSystemRequirement(RequirementItem item)
        {
            RemoveSystemRequirement(item.ItemID);
            systemRequirements.Add(item);            
        }

        public void RemoveSystemRequirement(string itemID)
        {
            int index = systemRequirements.FindIndex(x => x.ItemID == itemID);
            if (index >= 0)
            {
                systemRequirements.RemoveAt(index);
            }
        }

        public void UpdateSoftwareRequirement(RequirementItem item)
        {
            RemoveSoftwareRequirement(item.ItemID);
            softwareRequirements.Add(item);
        }

        public void RemoveSoftwareRequirement(string itemID)
        {
            int index = softwareRequirements.FindIndex(x => x.ItemID == itemID);
            if (index >= 0)
            {
                softwareRequirements.RemoveAt(index);
            }
        }

        public void UpdateRisk(RiskItem item)
        {
            RemoveRisk(item.ItemID);
            risks.Add(item);
        }

        public void RemoveRisk(string itemID)
        {
            int index = risks.FindIndex(x => x.ItemID == itemID);
            if (index >= 0)
            {
                risks.RemoveAt(index);
            }
        }

        public void UpdateSOUP(SOUPItem item)
        {
            RemoveSOUP(item.ItemID);
            soups.Add(item);
        }

        public void RemoveSOUP(string itemID)
        {
            int index = soups.FindIndex(x => x.ItemID == itemID);
            if (index >= 0)
            {
                soups.RemoveAt(index);
            }
        }

        public void UpdateSoftwareSystemTest(TestCaseItem item)
        {
            RemoveSoftwareSystemTest(item.ItemID);
            softwareSystemTests.Add(item);
        }

        public void RemoveSoftwareSystemTest(string itemID)
        {
            int index = softwareSystemTests.FindIndex(x => x.ItemID == itemID);
            if (index >= 0)
            {
                softwareSystemTests.RemoveAt(index);
            }
        }

        public void UpdateUnitTest(UnitTestItem item)
        {
            RemoveUnitTest(item.ItemID);
            unitTests.Add(item);
        }

        public void RemoveUnitTest(string itemID)
        {
            int index = unitTests.FindIndex(x => x.ItemID == itemID);
            if (index >= 0)
            {
                unitTests.RemoveAt(index);
            }
        }

        public void UpdateAnomaly(AnomalyItem item)
        {
            RemoveAnomaly(item.ItemID);
            anomalies.Add(item);
        }

        public void RemoveAnomaly(string itemID)
        {
            int index = anomalies.FindIndex(x => x.ItemID == itemID);
            if (index >= 0)
            {
                anomalies.RemoveAt(index);
            }
        }
    }
}
