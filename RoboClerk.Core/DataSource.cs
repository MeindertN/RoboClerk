namespace RoboClerk.Core
{
    public enum DataSource
    {
        SLMS, //the software lifecycle management system
        Source, //found in the source code
        Config, //found in the RoboClerk project configuration file
        OTS, //found in a binary control system
        Post, //tag is inserted to indicate insertion of data for post processing tools
        Comment, //comment tag, contents will be removed after processing
        Trace, //trace tag that is expected to be traced to this document
        Reference, //tag for referencing a document in another document
        Document, //tag for referencing a property of the document
        File, //a file in the template directory
        AI, //the AI system
        Web, //a web based service, can be a local webpage if so configured
        Unknown //it is not known where to retrieve this information
    }
}