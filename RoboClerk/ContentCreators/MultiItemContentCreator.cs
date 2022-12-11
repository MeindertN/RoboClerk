﻿using DocumentFormat.OpenXml.Spreadsheet;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.ContentCreators
{
    public abstract class MultiItemContentCreator : ContentCreatorBase
    {
        public MultiItemContentCreator(IDataSources data, ITraceabilityAnalysis analysis) 
            :base(data, analysis)
        {
        }

        protected abstract string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE);

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            var te = analysis.GetTraceEntityForAnyProperty(tag.ContentCreatorID);
            if (te == null)
            {
                throw new Exception($"Trace entity for content creator \"{tag.ContentCreatorID}\" is missing, this trace entity must be present for RoboClerk to function.");
            }
            bool foundContent = false;
            var items = data.GetItems(te);
            StringBuilder output = new StringBuilder();
            PropertyInfo[] properties = null;
            if(items.Count > 0)
            {
                properties = items[0].GetType().GetProperties();
            }
            List<LinkedItem> includedItems = new List<LinkedItem>();
            foreach (var item in items)
            {
                if (ShouldBeIncluded(tag, item, properties) && CheckUpdateDateTime(tag, item))
                {
                    foundContent = true;
                    includedItems.Add(item);
                }
            }
            try
            {
                return GenerateADocContent(tag, includedItems, te, analysis.GetTraceEntityForTitle(doc.DocumentTitle));
            }
            catch
            {
                logger.Error($"An error occurred while rendering {te.Name} in {doc.DocumentTitle}.");
                throw;
            }
        }
    }
}
