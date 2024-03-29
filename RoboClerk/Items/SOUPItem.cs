﻿using System;

namespace RoboClerk
{
    public class SOUPItem : LinkedItem
    {
        private string soupName = string.Empty;
        private string soupVersion = string.Empty;
        private bool soupLinkedLib = false;
        private string soupDetailedDescription = string.Empty;
        private string soupAnomalyListDescription = string.Empty;
        private string soupEnduserTraining = string.Empty;
        private string soupLicense = string.Empty;
        private bool soupInstalledByUser = false;
        private string soupInstalledByUserText = string.Empty;
        private bool soupPerformanceCritical = true;
        private string soupPerformanceCriticalText = string.Empty;
        private bool soupCybersecurityCritical = true;
        private string soupCybersecurityCriticalText = string.Empty;
        private string soupManufacturer = string.Empty;

        public SOUPItem()
        {
            type = "SOUP";
            id = Guid.NewGuid().ToString();
        }

        public string SOUPName
        {
            get { return soupName; }
            set { soupName = value; }
        }

        public string SOUPVersion
        {
            get { return soupVersion; }
            set { soupVersion = value; }
        }

        public bool SOUPLinkedLib
        {
            get { return soupLinkedLib; }
            set { soupLinkedLib = value; }
        }

        public string SOUPDetailedDescription
        {
            get { return soupDetailedDescription; }
            set { soupDetailedDescription = value; }
        }

        public string SOUPAnomalyListDescription
        {
            get { return soupAnomalyListDescription; }
            set { soupAnomalyListDescription = value; }
        }

        public string SOUPEnduserTraining
        {
            get { return soupEnduserTraining; }
            set { soupEnduserTraining = value; }
        }

        public string SOUPLicense
        {
            get { return soupLicense; }
            set { soupLicense = value; }
        }

        public bool SOUPInstalledByUser
        {
            get { return soupInstalledByUser; }
            set { soupInstalledByUser = value; }
        }

        public string SOUPInstalledByUserText
        {
            get { return soupInstalledByUserText; }
            set { soupInstalledByUserText = value; }
        }

        public bool SOUPPerformanceCritical
        {
            get { return soupPerformanceCritical; }
            set { soupPerformanceCritical = value; }
        }

        public string SOUPPerformanceCriticalText
        {
            get { return soupPerformanceCriticalText; }
            set { soupPerformanceCriticalText = value; }
        }

        public bool SOUPCybersecurityCritical
        {
            get { return soupCybersecurityCritical; }
            set { soupCybersecurityCritical = value; }
        }

        public string SOUPCybersecurityCriticalText
        {
            get { return soupCybersecurityCriticalText; }
            set { soupCybersecurityCriticalText = value; }
        }

        public string SOUPManufacturer
        {
            get { return soupManufacturer; }
            set { soupManufacturer = value; }
        }
    }
}
