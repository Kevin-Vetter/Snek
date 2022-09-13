using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Snek;

namespace Snek.DAL
{
    internal class DataStream
    {
        public DataStream()
        {
            _highScoreList = new ObservableCollection<SnekHighScore>();
        }
        private ObservableCollection<SnekHighScore> _highScoreList;

        public ObservableCollection<SnekHighScore> GetHighScoreList()
        {
            return _highScoreList;
        }
        public void SaveHighscoreList()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnekHighScore>));
            using (Stream writer = new FileStream("snek_highscorelist.xml", FileMode.Create))
            {
                serializer.Serialize(writer, _highScoreList);
            }
        }
        public void LoadHighscoreList()
        {
            if (File.Exists("snek_highscorelist.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SnekHighScore>));
                using (Stream reader = new FileStream("snek_highscorelist.xml", FileMode.Open))
                {
                    List<SnekHighScore> tempList = (List<SnekHighScore>)serializer.Deserialize(reader);
                    _highScoreList.Clear();
                    foreach (var item in tempList.OrderByDescending(x => x.Score))
                        _highScoreList.Add(item);
                }
            }
        }
    }
}
