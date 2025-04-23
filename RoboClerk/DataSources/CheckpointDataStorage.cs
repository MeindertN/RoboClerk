using DocumentFormat.OpenXml.Office2010.PowerPoint;
using System.Collections.Generic;

namespace RoboClerk
{
    public class CheckpointDataStorage
    {
        private List<RequirementItem> systemRequirements = new List<RequirementItem>();
        private List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        private List<RequirementItem> documentationRequirements = new List<RequirementItem>();
        private List<EliminatedRequirementItem> eliminatedSystemRequirements = new List<EliminatedRequirementItem>();
        private List<EliminatedRequirementItem> eliminatedSoftwareRequirements = new List<EliminatedRequirementItem>();
        private List<EliminatedRequirementItem> eliminatedDocumentationRequirements = new List<EliminatedRequirementItem>();
        private List<RiskItem> risks = new List<RiskItem>();
        private List<EliminatedRiskItem> eliminatedRisks = new List<EliminatedRiskItem>();
        private List<SOUPItem> soups = new List<SOUPItem>();
        private List<EliminatedSOUPItem> eliminatedSOUPs = new List<EliminatedSOUPItem>();
        private List<SoftwareSystemTestItem> softwareSystemTests = new List<SoftwareSystemTestItem>();
        private List<EliminatedSoftwareSystemTestItem> eliminatedSoftwareSystemTests = new List<EliminatedSoftwareSystemTestItem>();
        private List<UnitTestItem> unitTests = new List<UnitTestItem>();
        private List<AnomalyItem> anomalies = new List<AnomalyItem>();
        private List<EliminatedAnomalyItem> eliminatedAnomalies = new List<EliminatedAnomalyItem>();
        private List<DocContentItem> docContents = new List<DocContentItem>();
        private List<EliminatedDocContentItem> eliminatedDocContents = new List<EliminatedDocContentItem>();

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

        public List<EliminatedRequirementItem> EliminatedSystemRequirements
        {
            get
            {
                return eliminatedSystemRequirements;
            }
            set
            {
                eliminatedSystemRequirements = value;
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

        public List<EliminatedRequirementItem> EliminatedSoftwareRequirements
        {
            get
            {
                return eliminatedSoftwareRequirements;
            }
            set
            {
                eliminatedSoftwareRequirements = value;
            }
        }

        public List<RequirementItem> DocumentationRequirements
        {
            get
            {
                return documentationRequirements;
            }
            set
            {
                documentationRequirements = value;
            }
        }

        public List<EliminatedRequirementItem> EliminatedDocumentationRequirements
        {
            get
            { 
                return eliminatedDocumentationRequirements;
            }
            set
            {  
                eliminatedDocumentationRequirements = value;
            }
        }

        public List<DocContentItem> DocContents
        {
            get
            {
                return docContents;
            }
            set
            {
                docContents = value;
            }
        }

        public List<EliminatedDocContentItem> EliminatedDocContents
        {
            get
            {
                return eliminatedDocContents;
            }
            set
            {
                eliminatedDocContents = value;
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

        public List<EliminatedRiskItem> EliminatedRisks
        {
            get
            {
                return eliminatedRisks;
            }
            set
            {
                eliminatedRisks = value;
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

        public List<EliminatedSOUPItem> EliminatedSOUPs
        {
            get
            {
                return eliminatedSOUPs;
            }
            set
            {
                eliminatedSOUPs = value;
            }
        }

        public List<SoftwareSystemTestItem> SoftwareSystemTests
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

        public List<EliminatedSoftwareSystemTestItem> EliminatedSoftwareSystemTests
        {
            get
            {
                return eliminatedSoftwareSystemTests;
            }
            set
            {
                eliminatedSoftwareSystemTests = value;
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

        public List<EliminatedAnomalyItem> EliminatedAnomalies
        {
            get
            {
                return eliminatedAnomalies;
            }
            set
            {
                eliminatedAnomalies = value;
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

        public void UpdateDocumentationRequirement(RequirementItem item)
        {
            RemoveDocumentationRequirement(item.ItemID);
            documentationRequirements.Add(item);
        }

        public void RemoveDocumentationRequirement(string itemID)
        {
            int index = documentationRequirements.FindIndex(x => x.ItemID == itemID);
            if (index >= 0)
            {
                documentationRequirements.RemoveAt(index);
            }
        }

        public void UpdateDocContent(DocContentItem item)
        {
            RemoveDocContent(item.ItemID);
            docContents.Add(item);
        }

        public void RemoveDocContent(string itemID)
        {
            int index = docContents.FindIndex(x => x.ItemID == itemID);
            if (index >= 0)
            {
                docContents.RemoveAt(index);
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

        public void UpdateSoftwareSystemTest(SoftwareSystemTestItem item)
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
