﻿
using LiteDB;

namespace DiscordConnector.Records
{
    public class Position
    {
        public float x { get; }
        public float y { get; }
        public float z { get; }

        public Position()
        {
            x = 0;
            y = 0;
            z = 0;
        }
        public Position(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public override string ToString()
        {
            return $"({x},{y},{z})";
        }
    }
    public class SimpleStat
    {
        public ObjectId StatId { get; }
        public string Name { get; }
        public System.DateTime Date { get; }
        public string PlayerId { get; }
        public Position Pos { get; }

        public SimpleStat(string name, string playerHostName)
        {
            StatId = ObjectId.NewObjectId();
            Name = name;
            PlayerId = playerHostName;
            Date = System.DateTime.Now;
            Pos = new Position();
        }

        public SimpleStat(string name, string playerHostName, float x, float y, float z)
        {
            StatId = ObjectId.NewObjectId();
            Name = name;
            PlayerId = playerHostName;
            Date = System.DateTime.Now;
            Pos = new Position(x, y, z);
        }

        [BsonCtor]
        public SimpleStat(ObjectId _id, string name, System.DateTime date, string playerHostName, Position pos)
        {
            StatId = _id;
            Name = name;
            Date = date;
            PlayerId = playerHostName;
            Pos = pos;
        }

        public override string ToString()
        {
            return $"{Date.ToShortDateString()} {Date.ToShortTimeString()}: {Name} ({PlayerId}) at {Pos}";
        }
    }

}
