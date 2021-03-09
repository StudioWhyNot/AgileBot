using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgileBot.Models
{
    [Serializable]
    public class ListBoardsResponseInfo
    {
        public int maxResults;
        public int startAt;
        public bool isLast;
        public List<BoardInfo> values;
    }

    [Serializable]
    public class BoardInfo
    {
        public int id;
        public string self;
        public string name;
        public string type;
    }

    [Serializable]
    public class ProjectInfo
    {
        public string description;
    }
}
