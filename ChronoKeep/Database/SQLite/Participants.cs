﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Database.SQLite
{
    class Participants
    {
        internal static void AddParticipant(Participant person, SQLiteConnection connection)
        {
            person.FormatData();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO participants (participant_first, participant_last, participant_street, " +
                "participant_city, participant_state, participant_zip, participant_birthday, participant_email, participant_phone, " +
                "participant_mobile, participant_parent, participant_country, participant_street2, participant_gender, " +
                "emergencycontact_name, emergencycontact_phone)" +
                " VALUES (@first,@last,@street,@city,@state,@zip,@birthdate,@email,@phone,@mobile,@parent,@country,@street2," +
                "@gender,@ecname,@ecphone); SELECT participant_id FROM participants WHERE participant_first=@first " +
                "AND participant_last=@last AND participant_street=@street AND participant_city=@city AND " +
                "participant_state=@state AND participant_zip=@zip;";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@first", person.FirstName),
                new SQLiteParameter("@last", person.LastName),
                new SQLiteParameter("@street", person.Street),
                new SQLiteParameter("@city", person.City),
                new SQLiteParameter("@state", person.State),
                new SQLiteParameter("@zip", person.Zip),
                new SQLiteParameter("@birthdate", person.Birthdate),
                new SQLiteParameter("@email", person.Email),
                new SQLiteParameter("@phone", person.Phone),
                new SQLiteParameter("@mobile", person.Mobile),
                new SQLiteParameter("@parent", person.Parent),
                new SQLiteParameter("@country", person.Country),
                new SQLiteParameter("@street2", person.Street2),
                new SQLiteParameter("@ecname", person.ECName),
                new SQLiteParameter("@ecphone", person.ECPhone),
                new SQLiteParameter("@gender", person.Gender) });
            command.ExecuteNonQuery();
            person.Identifier = GetParticipantID(person, connection);
            command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO eventspecific (participant_id, event_id, distance_id, eventspecific_bib, " +
                "eventspecific_checkedin, eventspecific_comments, eventspecific_owes, eventspecific_other, " +
                "eventspecific_age_group_name, eventspecific_age_group_id, eventspecific_anonymous) " +
                "VALUES (@participant,@event,@distance,@bib,@checkedin,@comments,@owes,@other,@ageGroupName,@ageGroupId,@anon)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@participant", person.Identifier),
                new SQLiteParameter("@event", person.EventSpecific.EventIdentifier),
                new SQLiteParameter("@distance", person.EventSpecific.DistanceIdentifier),
                new SQLiteParameter("@bib", person.EventSpecific.Bib),
                new SQLiteParameter("@checkedin", person.EventSpecific.CheckedIn),
                new SQLiteParameter("@comments", person.EventSpecific.Comments),
                new SQLiteParameter("@owes", person.EventSpecific.Owes),
                new SQLiteParameter("@other", person.EventSpecific.Other),
                new SQLiteParameter("@ageGroupName", person.EventSpecific.AgeGroupName),
                new SQLiteParameter("@ageGroupId", person.EventSpecific.AgeGroupId),
                new SQLiteParameter("@anon", person.EventSpecific.Anonymous ? 1 : 0)
            });
            command.ExecuteNonQuery();
        }

        internal static void RemoveParticipant(int identifier, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM eventspecific WHERE participant_id=@0; DELETE FROM participants WHERE participant_id=@0";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", identifier) });
            command.ExecuteNonQuery();
        }

        internal static void RemoveParticipantEntry(int identifier, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM eventspecific WHERE participant_id=@0;";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", identifier) });
            command.ExecuteNonQuery();
        }

        internal static void RemoveEntry(int eventId, int participantId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM eventspecific WHERE participant_id=@participant AND event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@participant", participantId) });
            command.ExecuteNonQuery();
        }

        internal static void UpdateParticipant(Participant person, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE participants SET participant_first=@first, participant_last=@last, participant_street=@street," +
                " participant_city=@city, participant_state=@state, participant_zip=@zip, participant_birthday=@birthdate," +
                " emergencycontact_name=@ecname, emergencycontact_phone=@ecphone, participant_email=@email, participant_phone=@phone, participant_mobile=@mobile," +
                " participant_parent=@parent, participant_country=@country, participant_street2=@street2, participant_gender=@gender WHERE participant_id=@participantid";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@first", person.FirstName),
                    new SQLiteParameter("@last", person.LastName),
                    new SQLiteParameter("@street", person.Street),
                    new SQLiteParameter("@city", person.City),
                    new SQLiteParameter("@state", person.State),
                    new SQLiteParameter("@zip", person.Zip),
                    new SQLiteParameter("@birthdate", person.Birthdate),
                    new SQLiteParameter("@ecname", person.ECName),
                    new SQLiteParameter("@ecphone", person.ECPhone),
                    new SQLiteParameter("@email", person.Email),
                    new SQLiteParameter("@participantid", person.Identifier),
                    new SQLiteParameter("@phone", person.Phone),
                    new SQLiteParameter("@mobile", person.Mobile),
                    new SQLiteParameter("@parent", person.Parent),
                    new SQLiteParameter("@country", person.Country),
                    new SQLiteParameter("@street2", person.Street2),
                    new SQLiteParameter("@gender", person.Gender) });
            command.ExecuteNonQuery();
            command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE eventspecific SET distance_id=@distanceId, eventspecific_bib=@bib, eventspecific_checkedin=@checkedin, " +
                "eventspecific_owes=@owes, eventspecific_other=@other, " +
                "eventspecific_comments=@comments, eventspecific_status=@status, eventspecific_age_group_name=@ageGroupName, eventspecific_age_group_id=@ageGroupId, " +
                "eventspecific_anonymous=@anon " +
                "WHERE eventspecific_id=@eventspecid";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@distanceId", person.EventSpecific.DistanceIdentifier),
                    new SQLiteParameter("@bib", person.EventSpecific.Bib),
                    new SQLiteParameter("@checkedin", person.EventSpecific.CheckedIn),
                    new SQLiteParameter("@eventspecid", person.EventSpecific.Identifier),
                    new SQLiteParameter("@owes", person.EventSpecific.Owes),
                    new SQLiteParameter("@other", person.EventSpecific.Other),
                    new SQLiteParameter("@comments", person.EventSpecific.Comments),
                    new SQLiteParameter("@status", person.EventSpecific.Status),
                    new SQLiteParameter("@ageGroupName", person.EventSpecific.AgeGroupName),
                    new SQLiteParameter("@ageGroupId", person.EventSpecific.AgeGroupId),
                    new SQLiteParameter("@anon", person.EventSpecific.Anonymous ? 1 : 0)
                });
            command.ExecuteNonQuery();
        }

        internal static void V44UpdateParticipant(Participant person, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE participants SET participant_first=@first, participant_last=@last, participant_street=@street," +
                " participant_city=@city, participant_state=@state, participant_zip=@zip, participant_birthday=@birthdate," +
                " emergencycontact_name=@ecname, emergencycontact_phone=@ecphone, participant_email=@email, participant_mobile=@mobile," +
                " participant_parent=@parent, participant_country=@country, participant_street2=@street2, participant_gender=@gender WHERE participant_id=@participantid";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@first", person.FirstName),
                    new SQLiteParameter("@last", person.LastName),
                    new SQLiteParameter("@street", person.Street),
                    new SQLiteParameter("@city", person.City),
                    new SQLiteParameter("@state", person.State),
                    new SQLiteParameter("@zip", person.Zip),
                    new SQLiteParameter("@birthdate", person.Birthdate),
                    new SQLiteParameter("@ecname", person.ECName),
                    new SQLiteParameter("@ecphone", person.ECPhone),
                    new SQLiteParameter("@email", person.Email),
                    new SQLiteParameter("@participantid", person.Identifier),
                    new SQLiteParameter("@mobile", person.Mobile),
                    new SQLiteParameter("@parent", person.Parent),
                    new SQLiteParameter("@country", person.Country),
                    new SQLiteParameter("@street2", person.Street2),
                    new SQLiteParameter("@gender", person.Gender) });
            command.ExecuteNonQuery();
            command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE eventspecific SET distance_id=@distanceId, eventspecific_bib=@bib, eventspecific_checkedin=@checkedin, " +
                "eventspecific_owes=@owes, eventspecific_other=@other, " +
                "eventspecific_comments=@comments, eventspecific_status=@status, eventspecific_age_group_name=@ageGroupName, eventspecific_age_group_id=@ageGroupId " +
                "WHERE eventspecific_id=@eventspecid";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@distanceId", person.EventSpecific.DistanceIdentifier),
                    new SQLiteParameter("@bib", person.EventSpecific.Bib),
                    new SQLiteParameter("@checkedin", person.EventSpecific.CheckedIn),
                    new SQLiteParameter("@eventspecid", person.EventSpecific.Identifier),
                    new SQLiteParameter("@owes", person.EventSpecific.Owes),
                    new SQLiteParameter("@other", person.EventSpecific.Other),
                    new SQLiteParameter("@comments", person.EventSpecific.Comments),
                    new SQLiteParameter("@status", person.EventSpecific.Status),
                    new SQLiteParameter("@ageGroupName", person.EventSpecific.AgeGroupName),
                    new SQLiteParameter("@ageGroupId", person.EventSpecific.AgeGroupId)
                });
            command.ExecuteNonQuery();
        }

        internal static List<Participant> GetParticipants(SQLiteConnection connection)
        {
            Log.D("SQLite.Participants", "Getting all participants for all events.");
            return GetParticipantsWorker("SELECT * FROM participants p " +
                "JOIN eventspecific s ON p.participant_id = s.participant_id " +
                "LEFT JOIN bib_chip_assoc c ON c.bib = s.eventspecific_bib AND c.event_id=s.event_id " +
                "JOIN distances d ON s.distance_id = d.distance_id ORDER BY p.participant_last ASC, p.participant_first ASC", -1, -1, connection);
        }

        internal static List<Participant> GetParticipants(int eventId, SQLiteConnection connection)
        {
            Log.D("SQLite.Participants", "Getting all participants for event with id of " + eventId);
            return GetParticipantsWorker("SELECT * FROM participants p " +
                "JOIN eventspecific s ON p.participant_id = s.participant_id " +
                "JOIN distances d ON s.distance_id = d.distance_id " +
                "LEFT JOIN bib_chip_assoc c ON c.bib = s.eventspecific_bib AND c.event_id=s.event_id " +
                "WHERE s.event_id=@event ORDER BY p.participant_last ASC, p.participant_first ASC", eventId, -1, connection);
        }

        internal static List<Participant> GetParticipants(int eventId, int distanceId, SQLiteConnection connection)
        {
            Log.D("SQLite.Participants", "Getting all participants for event with id of " + eventId);
            return GetParticipantsWorker("SELECT * FROM participants p " +
                "JOIN eventspecific s ON p.participant_id = s.participant_id " +
                "JOIN distances d ON s.distance_id = d.distance_id " +
                "LEFT JOIN bib_chip_assoc c ON c.bib = s.eventspecific_bib AND c.event_id=s.event_id " +
                "WHERE s.event_id=@event AND d.distance_id=@distance ORDER BY p.participant_last ASC, p.participant_first ASC", eventId, distanceId, connection);
        }

        internal static List<Participant> GetParticipantsWorker(string query, int eventId, int distanceId, SQLiteConnection connection)
        {
            List<Participant> output = new List<Participant>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = query;
            if (eventId != -1)
            {
                command.Parameters.Add(new SQLiteParameter("@event", eventId));
            }
            if (distanceId != -1)
            {
                command.Parameters.Add(new SQLiteParameter("@distance", distanceId));
            }
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Participant(
                    Convert.ToInt32(reader["participant_id"]),
                    reader["participant_first"].ToString(),
                    reader["participant_last"].ToString(),
                    reader["participant_street"].ToString(),
                    reader["participant_city"].ToString(),
                    reader["participant_state"].ToString(),
                    reader["participant_zip"].ToString(),
                    reader["participant_birthday"].ToString(),
                    new EventSpecific(
                        Convert.ToInt32(reader["eventspecific_id"]),
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["distance_id"]),
                        reader["distance_name"].ToString(),
                        reader["eventspecific_bib"].ToString(),
                        Convert.ToInt32(reader["eventspecific_checkedin"]),
                        reader["eventspecific_comments"].ToString(),
                        reader["eventspecific_owes"].ToString(),
                        reader["eventspecific_other"].ToString(),
                        Convert.ToInt32(reader["eventspecific_status"]),
                        reader["eventspecific_age_group_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_age_group_id"]),
                        Convert.ToInt16(reader["eventspecific_anonymous"]) == 0 ? false : true
                        ),
                    reader["participant_email"].ToString(),
                    reader["participant_phone"].ToString(),
                    reader["participant_mobile"].ToString(),
                    reader["participant_parent"].ToString(),
                    reader["participant_country"].ToString(),
                    reader["participant_street2"].ToString(),
                    reader["participant_gender"].ToString(),
                    reader["emergencycontact_name"].ToString(),
                    reader["emergencycontact_phone"].ToString(),
                    reader["chip"].ToString()
                    ));
            }
            reader.Close();
            return output;
        }

        internal static Participant GetParticipantWorker(SQLiteDataReader reader)
        {
            if (reader.Read())
            {
                return new Participant(
                    Convert.ToInt32(reader["participant_id"]),
                    reader["participant_first"].ToString(),
                    reader["participant_last"].ToString(),
                    reader["participant_street"].ToString(),
                    reader["participant_city"].ToString(),
                    reader["participant_state"].ToString(),
                    reader["participant_zip"].ToString(),
                    reader["participant_birthday"].ToString(),
                    new EventSpecific(
                        Convert.ToInt32(reader["eventspecific_id"]),
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["distance_id"]),
                        reader["distance_name"].ToString(),
                        reader["eventspecific_bib"].ToString(),
                        Convert.ToInt32(reader["eventspecific_checkedin"]),
                        reader["eventspecific_comments"].ToString(),
                        reader["eventspecific_owes"].ToString(),
                        reader["eventspecific_other"].ToString(),
                        Convert.ToInt32(reader["eventspecific_status"]),
                        reader["eventspecific_age_group_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_age_group_id"]),
                        Convert.ToInt16(reader["eventspecific_anonymous"]) == 0 ? false : true
                        ),
                    reader["participant_email"].ToString(),
                    reader["participant_phone"].ToString(),
                    reader["participant_mobile"].ToString(),
                    reader["participant_parent"].ToString(),
                    reader["participant_country"].ToString(),
                    reader["participant_street2"].ToString(),
                    reader["participant_gender"].ToString(),
                    reader["emergencycontact_name"].ToString(),
                    reader["emergencycontact_phone"].ToString(),
                    reader["chip"].ToString()
                    );
            }
            return null;
        }

        internal static Participant GetParticipantEventSpecific(int eventIdentifier, int eventSpecificId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM participants AS p " +
                "JOIN eventspecific AS s ON p.participant_id=s.participant_id " +
                "JOIN distances AS d ON s.distance_id=d.distance_id " +
                "JOIN bib_chip_assoc c ON c.bib = s.eventspecific_bib " +
                "WHERE s.event_id=@eventid " +
                "AND s.eventspecific_id=@eventSpecId";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventIdentifier));
            command.Parameters.Add(new SQLiteParameter("@eventSpecId", eventSpecificId));
            SQLiteDataReader reader = command.ExecuteReader();
            Participant output = GetParticipantWorker(reader);
            reader.Close();
            return output;
        }

        internal static Participant GetParticipantBib(int eventIdentifier, string bib, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM participants AS p " +
                "JOIN eventspecific AS s ON p.participant_id=s.participant_id " +
                "JOIN distances AS d ON s.distance_id=d.distance_id " +
                "JOIN bib_chip_assoc c ON c.bib = s.eventspecific_bib " +
                "WHERE s.event_id=@eventid " +
                "AND s.eventspecific_bib=@bib";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventIdentifier));
            command.Parameters.Add(new SQLiteParameter("@bib", bib));
            SQLiteDataReader reader = command.ExecuteReader();
            Participant output = GetParticipantWorker(reader);
            reader.Close();
            return output;
        }

        internal static Participant GetParticipant(int eventId, int identifier, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM participants AS p " +
                "JOIN eventspecific AS s ON p.participant_id=s.participant_id " +
                "JOIN distances AS d ON s.distance_id=d.distance_id " +
                "JOIN bib_chip_assoc c ON c.bib = s.eventspecific_bib " +
                "WHERE s.event_id=@eventid AND p.participant_id=@partId";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventId));
            command.Parameters.Add(new SQLiteParameter("@partId", identifier));
            SQLiteDataReader reader = command.ExecuteReader();
            Participant output = GetParticipantWorker(reader);
            reader.Close();
            return output;
        }

        internal static Participant GetParticipant(int eventId, Participant unknown, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            if (unknown.EventSpecific.Chip != -1)
            {
                command.CommandText = "SELECT * FROM participants AS p, eventspecific AS s, distances AS d, " +
                    "bib_chip_assoc as b WHERE p.participant_id=s.participant_id AND s.event_id=@eventid " +
                    "AND d.distance_id=s.distance_id AND " +
                    "s.eventspecific_bib=b.bib AND b.chip=@chip AND b.event_id=s.event_id;";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@eventid", eventId),
                    new SQLiteParameter("@chip", unknown.EventSpecific.Chip),
                });
            }
            else
            {
                command.CommandText =
                command.CommandText = "SELECT * FROM participants AS p " +
                    "JOIN eventspecific AS s ON p.participant_id=s.participant_id " +
                    "JOIN distances AS d ON s.distance_id=d.distance_id " +
                    "JOIN bib_chip_assoc c ON c.bib = s.eventspecific_bib " +
                    "WHERE s.event_id=@eventid " +
                    "AND p.participant_first=@first AND p.participant_last=@last AND p.participant_street=@street " +
                    "AND p.participant_city=@city AND p.participant_state=@state AND p.participant_zip=@zip " +
                    "AND p.participant_birthday=@birthday";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@eventid", eventId),
                    new SQLiteParameter("@first", unknown.FirstName),
                    new SQLiteParameter("@last", unknown.LastName),
                    new SQLiteParameter("@street", unknown.Street),
                    new SQLiteParameter("@city", unknown.City),
                    new SQLiteParameter("@state", unknown.State),
                    new SQLiteParameter("@zip", unknown.Zip),
                    new SQLiteParameter("@birthday", unknown.Birthdate)
                });

            }
            SQLiteDataReader reader = command.ExecuteReader();
            Participant output = GetParticipantWorker(reader);
            reader.Close();
            return output;
        }

        internal static int GetParticipantID(Participant person, SQLiteConnection connection)
        {
            int output = -1;
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT participant_id FROM participants WHERE participant_first=@first AND " +
                "participant_last=@last AND participant_street=@street AND " +
                "participant_zip=@zip AND participant_birthday=@birthday";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@first", person.FirstName),
                new SQLiteParameter("@last", person.LastName),
                new SQLiteParameter("@street", person.Street),
                new SQLiteParameter("@zip", person.Zip),
                new SQLiteParameter("@birthday", person.Birthdate)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                try
                {
                    output = Convert.ToInt32(reader["participant_id"]);
                }
                catch
                {
                    output = -1;
                }
            }
            reader.Close();
            return output;
        }

    }
}
