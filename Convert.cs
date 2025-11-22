namespace CurrencyLibrary.Utility
{
    public class Convertcurrency
    {
        public static double onecurrencytoother(double from, double to, double amount)
        {
            double res = to / from;
            if (amount == 1.00d)
                return res;
            else
                return res * amount;
            //return Math.Round(res * amount);
        }
    }
}
