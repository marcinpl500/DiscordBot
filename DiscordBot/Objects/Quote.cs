using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Database;
using DiscordBot.Extensions;
using MySql.Data.MySqlClient;

namespace DiscordBot.Objects
{
    public class Quote
    {
        internal static List<Quote> Quotes;
        
        internal int QId;
        internal ulong CreatorId, AcceptedBy, CreatedIn, AcceptedIn;
        internal string QuoteText;
        internal DateTime QCreatedTimestamp;

        private Quote() { }
        
        internal Quote(int quoteId, ulong createdBy, ulong acceptedBy, string quote, DateTime createdTimestamp, ulong createdIn, ulong acceptedIn)
        {
            QId = quoteId;
            CreatorId = createdBy;
            AcceptedBy = acceptedBy;
            QuoteText = quote;
            QCreatedTimestamp = createdTimestamp;
            CreatedIn = createdIn;
            AcceptedIn = acceptedIn;
        }
        
        internal static List<Quote> LoadAll()
        {
            List<Quote> quotes = new List<Quote>();
            (MySqlDataReader dr, MySqlConnection conn) reader = DatabaseActivity.ExecuteReader("SELECT * FROM quotes;");
            
            while (reader.dr.Read())
            {
                Quote q = new Quote
                {
                    QId = reader.dr.GetInt32("quoteId"),
                    CreatorId = reader.dr.GetUInt64("createdBy"),
                    AcceptedBy = reader.dr.GetUInt64("acceptedBy"),
                    QuoteText = reader.dr.GetString("quoteText"),
                    QCreatedTimestamp = reader.dr.GetDateTime("dateCreated"),
                    CreatedIn = reader.dr.GetUInt64("createdIn"),
                    AcceptedIn = reader.dr.GetUInt64("acceptedIn")
                };

                quotes.Add(q);
            }
            
            reader.dr.Close();
            reader.conn.Close();

            return quotes;
        }

        internal static bool AddQuote(string quote, ulong creatorId, ulong createdIn, ulong acceptedBy = 0, ulong acceptedIn = 0, DateTime? createdTimestamp = null)
        {
            if (acceptedBy == 0) { acceptedBy = creatorId; }
            if (acceptedIn == 0) { acceptedIn = createdIn; }
            
            List<(string, string)> queryParams = new List<(string id, string value)>()
            {
                ("@createdBy", creatorId.ToString()),
                ("@acceptedBy", acceptedBy.ToString()),
                ("@quoteText", quote),
                ("@createdIn", createdIn.ToString()),
                ("@acceptedIn", acceptedIn.ToString())
            };
            
            int rowsUpdated = DatabaseActivity.ExecuteNonQueryCommand(
                "INSERT IGNORE INTO " +
                "quotes(quoteId,createdBy,acceptedBy,quoteText,dateCreated,createdIn,acceptedIn) " +
                "VALUES (NULL, @createdBy, @acceptedBy, @quoteText, CURRENT_TIMESTAMP, @createdIn, @acceptedIn);", queryParams);
            
            // Add quote to the current loaded quote-list
            (MySqlDataReader dr, MySqlConnection conn) reader = DatabaseActivity.ExecuteReader("SELECT * FROM quotes ORDER BY quoteId DESC LIMIT 1");
            int newId = 0;
            while (reader.dr.Read()) { newId = reader.dr.GetInt32("quoteId"); }
            reader.dr.Close();
            reader.conn.Close();
            
            Quotes.Add(new Quote
            {
                QId = newId,
                CreatorId = creatorId,
                AcceptedBy = acceptedBy,
                QuoteText = quote,
                QCreatedTimestamp = DateTime.Now,
                CreatedIn = createdIn,
                AcceptedIn = acceptedIn
            });
            
            return rowsUpdated == 1;
        }

        internal static bool UpdateQuote(int quoteId, string quoteText)
        {
            Quote quote = Quotes.Find(q => q.QId == quoteId);
            int index = Quotes.IndexOf(Quotes.Find(q => q.QId == quoteId));
            quote.QuoteText = quoteText;
            Quotes[index] = quote;

            string formattedText = quoteText.Replace("\"", "\\\"");
            
            int rowsAffected = DatabaseActivity.ExecuteNonQueryCommand(
                "UPDATE quotes SET `quoteText`=\"" + formattedText + "\" WHERE `quoteId`=" + quoteId +";");

            return rowsAffected == 1;
        }

        internal static bool DeleteQuote(int quoteId)
        {
            Quotes.Remove(Quotes.Find(quote => quote.QId == quoteId));
            
            int rowsAffected = DatabaseActivity.ExecuteNonQueryCommand(
                "DELETE FROM quotes WHERE quoteId=" + quoteId + ";");
            
            return rowsAffected == 1;
        }
    }
}