using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Data;
using Agresso.ServerExtension;
using Agresso.Interface.CommonExtension;

namespace OppdaterPostnummer
{
    [ServerProgram("POSTNR")]
    public class Postnumber : ServerProgramBase
    {
        public override void Run()
        {
            string client = ServerAPI.Current.Parameters["client"];
            string url = ServerAPI.Current.Parameters["url"];
            string html = string.Empty;
            if (url == string.Empty)
                Me.StopReport("Ingen url");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) 
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(1252)))
            {
                html = reader.ReadToEnd();
            }
            DataTable dataTable = new DataTable("Postnummber");
            IServerDbAPI api = ServerAPI.Current.DatabaseAPI;
            IStatement sql = CurrentContext.Database.CreateStatement();
            sql.Append("Select * from aagzipcodes where country_code = 'NO'");
            CurrentContext.Database.Read(sql, dataTable);
            if (html != string.Empty)
            {
                Me.API.WriteLog("Sjekker om det er noe nytt");
                String[] lines = html.Split('\n');
                foreach (string line in lines)
                {
                    String[] words = line.Split('\t');
                    bool zipcode = false;
                    bool name = false;
                    bool komunr = false;
                    IStatement postsql = CurrentContext.Database.CreateStatement();
                    if (words.Length > 2)
                    {
                        foreach (DataRow row in dataTable.Rows)
                        {

                            if (row["zip_code"].ToString().ToFixedString(4, '0') == words[0])
                                zipcode = true;
                            if (row["place"].ToString() == @words[1])
                                name = true;
                            if (row["municipal"].ToString().ToFixedString(4, '0') == words[2])
                                komunr = true;

                        }
                        if (!zipcode)
                        {
                            postsql.Append("insert into aagzipcodes(country_code, zip_code, place,municipal, user_id, last_update) values ('NO','");
                            postsql.Append(words[0]);
                            postsql.Append("','");
                            postsql.Append(@words[1]);
                            postsql.Append("','");
                            postsql.Append(words[2]);
                            postsql.Append("','POSTNR',getDate())");
                            Me.API.WriteLog("Lagt til postkode: {0}", words[0]);
                        }
                        else
                        {
                            if (!name || !komunr)
                            {
                                postsql.Append("update aagzipcodes set place ='");
                                postsql.Append(@words[1]);
                                postsql.Append("',municipal='");
                                postsql.Append(words[2]);
                                postsql.Append("',user_id='POSTNR', last_update = getDate() where country_code = 'NO' and zip_code ='");
                                postsql.Append(words[0]);
                                postsql.Append("'");
                                Me.API.WriteLog("Oppdatert postkode: {0}", words[0]);
                            }


                        }
                        if (!postsql.IsEmpty())
                            CurrentContext.Database.Execute(postsql);
                    }
                }
            }


        }

    }
}
public static class StringExtensions
{
    /// <summary>
    /// Extends the <code>String</code> class with this <code>ToFixedString</code> method.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="length">The prefered fixed string size</param>
    /// <param name="appendChar">The <code>char</code> to append</param>
    /// <returns></returns>
    public static String ToFixedString(this String value, int length, char appendChar = ' ')
    {
        int currlen = value.Length;
        int needed = length == currlen ? 0 : (length - currlen);

        return needed == 0 ? value :
            (needed > 0 ? value + new string(' ', needed) :
                new string(new string(value.ToCharArray().Reverse().ToArray()).
                    Substring(needed * -1, value.Length - (needed * -1)).ToCharArray().Reverse().ToArray()));
    }
}
