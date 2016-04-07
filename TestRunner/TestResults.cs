namespace TestRunner
{
    public class TestResults
    {
        public string TrxFile { get; set; }
        public string TrxHtmlFile { get; set; }
        public string CovFile { get; set; }
        public string CovXmlFile { get; set; }
        public string CovHtmlFile { get; set; }
        public string SummaryHtmlFile { get; set; }
        public string MtmUrl { get; set; }
        public string TestCategory { get; set; }
        public int ExecutionOrder { get; set; }
    }
}
