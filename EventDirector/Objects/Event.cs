using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Event
    {
        private int identifier, nextYear = -1, shirtOptional = 1, shirtPrice = 2000;
        private int common_age_groups = 1, common_start_finish = 1, division_specific_segments = 0, rank_by_gun = 1;
        private int allow_early_start = 0, early_start_difference = -1;
        private string name, date, yearcode = "";

        public Event() { }

        public Event(string n, long d, int so, int price)
        {
            this.shirtOptional = so;
            this.date = new DateTime(d).ToShortDateString();
            this.name = n;
            this.shirtPrice = price;
        }

        public Event(string n, long d, int so, int price, string yearcode)
        {
            this.shirtOptional = so;
            this.date = new DateTime(d).ToShortDateString();
            this.name = n;
            this.shirtPrice = price;
            this.yearcode = yearcode;
        }

        public Event(int id, string n, long d, int ny, int so, int price)
        {
            this.nextYear = ny;
            this.identifier = id;
            this.shirtOptional = so;
            this.name = n;
            this.date = new DateTime(d).ToShortDateString();
            this.shirtPrice = price;
        }

        public Event(string n, long d, int so, int price, int age, int start, int seg, int gun)
        {
            this.shirtOptional = so;
            this.date = new DateTime(d).ToShortDateString();
            this.name = n;
            this.shirtPrice = price;
            this.common_age_groups = age;
            this.common_start_finish = start;
            this.division_specific_segments = seg;
            this.rank_by_gun = gun;
        }

        public Event(int id, string n, long d, int ny, int so, int price, int age, int start, int seg, int gun)
        {
            this.nextYear = ny;
            this.identifier = id;
            this.shirtOptional = so;
            this.name = n;
            this.date = new DateTime(d).ToShortDateString();
            this.shirtPrice = price;
            this.common_age_groups = age;
            this.common_start_finish = start;
            this.division_specific_segments = seg;
            this.rank_by_gun = gun;
        }

        public Event(int id, string n, string d, int ny, int so, int price, int age, int start, int seg, int gun, string yearcode, int early, int earlydiff)
        {
            this.nextYear = ny;
            this.identifier = id;
            this.shirtOptional = so;
            this.name = n;
            this.date = d;
            this.shirtPrice = price;
            this.common_age_groups = age;
            this.common_start_finish = start;
            this.division_specific_segments = seg;
            this.rank_by_gun = gun;
            this.yearcode = yearcode;
            this.allow_early_start = early;
            this.early_start_difference = earlydiff;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int NextYear { get => nextYear; set => nextYear = value; }
        public string Name { get => name; set => name = value; }
        public string Date { get => date; set => date = value; }
        public int ShirtOptional { get => shirtOptional; set => shirtOptional = value; }
        public int ShirtPrice { get => shirtPrice; set => shirtPrice = value; }
        public int CommonAgeGroups { get => common_age_groups; set => common_age_groups = value; }
        public int CommonStartFinish { get => common_start_finish; set => common_start_finish = value; }
        public int DivisionSpecificSegments { get => division_specific_segments; set => division_specific_segments = value; }
        public int RankByGun { get => rank_by_gun; set => rank_by_gun = value; }
        public string YearCode { get => yearcode; set => yearcode = value; }
        public int AllowEarlyStart { get => allow_early_start; set => allow_early_start = value; }
        public int EarlyStartDifference { get => early_start_difference; set => early_start_difference = value; }

        public string GetEarlyStartString()
        {
            int hours = early_start_difference / 3600;
            int minutes = (early_start_difference % 3600) / 60;
            int seconds = early_start_difference % 60;
            if (early_start_difference < 0)
            {
                hours = minutes = seconds = 0;
            }
            return String.Format("{0,2:D2}:{1,2:D2}:{2,2:D2}", hours, minutes, seconds);
        }
    }
}
