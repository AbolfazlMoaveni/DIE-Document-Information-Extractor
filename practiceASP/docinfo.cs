namespace practiceASP
{
    public class DocInfo
    {
        public string Name { get; set; }
        public int NumPages { get; set; }
        public string Text { get; set; }

        public int NumPicPages { get; set; }
        public int NumTextPages { get; set; }
        public List<int> PicBasedPages { get; set; } = new List<int>();
        public List<int> TextBasedPages { get; set; } = new List<int>();

        public DocInfo(string name, int nump, string text)
        {
            Name = name;
            NumPages = nump;
            Text = text;
        }
        public DocInfo(string name, int nump, string text,int numPicPages,int numTextPages, List<int> picBasedPages, List<int> textBasedPages)
        {
            Name = name;
            NumPages = nump;
            Text = text;
            NumPicPages = numPicPages;
            NumTextPages = numTextPages;
            PicBasedPages = picBasedPages;
            TextBasedPages = textBasedPages;
        }
    }
}