using System;
using System.Globalization;
using System.IO;

namespace PortfolioValuation
{
    class Program
    {
        // The holdings are five arrays because a holding carries five different facts
        // and I need all of them row by row to value it
        // The prices are only two arrays because the only thing I ever do with a price
        // is look it up by ticker. In Python that was one dictionary
        // Arrays are created empty here so that the objects exists first to be altered later.
        static string[] tickers = new string[0];
        static string[] names = new string[0];
        static int[] quantities = new int[0];
        static double[] averageCosts = new double[0];
        static string[] currencies = new string[0];

        static string[] priceTickers = new string[0];
        static double[] closes = new double[0];

        static bool[] hasPrice = new bool[0];
        static double[] priceZar = new double[0];
        static double[] costZar = new double[0];
        static double[] valueZar = new double[0];
        static double[] pnlZar = new double[0];
        static double[] returnPct = new double[0];
        static double[] weightPct = new double[0];
        static string[] flags = new string[0];

// readonly is used instead of const because this is an array.
// The array itself can't be replaced with a different array,
// but the values inside the array can still be changed.

        static readonly string[] FxCurrencies = { "ZAR", "USD" };
        static readonly double[] FxRates = { 1.0, 17.85 };

        const double ConcentrationLimit = 25.0;
        const double GrowthRate = 0.08;
        const double DefaultTarget = 1000000.0;


        static int CountDataLines(string[] lines)
        {
            int count = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() != "")
                {
                    count++;
                }
            }

            return count;
        }

        static void ReadHoldings(string filename)
        {
            string[] lines = File.ReadAllLines(filename);

            string[] headings = lines[0].Split(',');
            int headingsLen = headings.Length;

            int holdingsLen = CountDataLines(lines);

            tickers = new string[holdingsLen];
            names = new string[holdingsLen];
            quantities = new int[holdingsLen];
            averageCosts = new double[holdingsLen];
            currencies = new string[holdingsLen];

            int row = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() != "")
                {
                    string[] lineContent = lines[i].Split(',');

                    for (int j = 0; j < headingsLen; j++)
                    {
                        string heading = headings[j].Trim().ToLower();
                        string value = lineContent[j].Trim();

                        if (heading == "ticker")
                        {
                            tickers[row] = value.ToUpper();
                        }
                        else if (heading == "name")
                        {
                            names[row] = value;
                        }
                        else if (heading == "quantity")
                        {
                            quantities[row] = int.Parse(value, CultureInfo.InvariantCulture);
                        }
                        else if (heading == "average_cost")
                        {
                            averageCosts[row] = double.Parse(value, CultureInfo.InvariantCulture);
                        }
                        else if (heading == "currency")
                        {
                            currencies[row] = value.ToUpper();
                        }
                    }

                    row++;
                }
            }
        }

        static void ReadPrices(string filename)
        {
            string[] lines = File.ReadAllLines(filename);

            int pricesLen = CountDataLines(lines);

            priceTickers = new string[pricesLen];
            closes = new double[pricesLen];

            int row = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() != "")
                {
                    string[] lineContent = lines[i].Replace(" ", "").ToUpper().Split(',');

                    priceTickers[row] = lineContent[0];
                    closes[row] = double.Parse(lineContent[1], CultureInfo.InvariantCulture);

                    row++;
                }
            }
        }

        // Phase 3

        // The tool Python gave me for free: "MSFT" in prices was one keyword in a dictionary
        // but here it is a loop through an array to find the index of that keyword then use that index
        // to get the corresponding value in another array.
        static int IndexOf(string[] values, string wanted)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == wanted)
                {
                    return i;
                }
            }

            return -1;
        }

        static double ToRands(double amount, string currency)
        {
            int rateRow = IndexOf(FxCurrencies, currency);

            return FxRates[rateRow] * amount;
        }

        static void ValueHolding(int i, double price)
        {
            int quantity = quantities[i];
            double priceInRands = ToRands(price, currencies[i]);
            double cost = ToRands(averageCosts[i], currencies[i]) * quantity;
            double value = priceInRands * quantity;
            double pnl = value - cost;

            priceZar[i] = priceInRands;
            costZar[i] = cost;
            valueZar[i] = value;
            pnlZar[i] = pnl;
            returnPct[i] = pnl / cost * 100;
            hasPrice[i] = true;
        }

        static void ValuePortfolio()
        {
            int holdingsLen = tickers.Length;

            hasPrice = new bool[holdingsLen];
            priceZar = new double[holdingsLen];
            costZar = new double[holdingsLen];
            valueZar = new double[holdingsLen];
            pnlZar = new double[holdingsLen];
            returnPct = new double[holdingsLen];

            for (int i = 0; i < holdingsLen; i++)
            {
                int priceRow = IndexOf(priceTickers, tickers[i]);

                if (priceRow != -1)
                {
                    ValueHolding(i, closes[priceRow]);
                }
                else
                {
                    Console.WriteLine($"WARNING: no price for {tickers[i]}, skipping it");
                }
            }
        }

        //Phase 4


        static double Sum(double[] values)
        {
            double total = 0;

            for (int i = 0; i < values.Length; i++)
            {
                total = total + values[i];
            }

            return total;
        }

        static double TotalCost()
        {
            return Sum(costZar);
        }

        static double TotalValue()
        {
            return Sum(valueZar);
        }

        static double TotalPnl()
        {
            return Sum(pnlZar);
        }

        static double TotalReturn()
        {
            return TotalPnl() / TotalCost() * 100;
        }

        static void AddPortfolioWeights()
        {
            double totalPortfolioValue = TotalValue();
            int holdingsLen = tickers.Length;

            weightPct = new double[holdingsLen];
            flags = new string[holdingsLen];

            for (int i = 0; i < holdingsLen; i++)
            {
                weightPct[i] = valueZar[i] / totalPortfolioValue * 100;
                if (weightPct[i] > ConcentrationLimit)
                {
                    flags[i] = "CONCENTRATED";
                }
                else
                {
                    flags[i] = "";
                }
            }
        }

        static int BestIndex()
        {
            int best = -1;

            for (int i = 0; i < tickers.Length; i++)
            {
                if (hasPrice[i])
                {
                    if (best == -1 || returnPct[i] > returnPct[best])
                    {
                        best = i;
                    }
                }
            }

            return best;
        }

        static int WorstIndex()
        {
            int worst = -1;

            for (int i = 0; i < tickers.Length; i++)
            {
                if (hasPrice[i])
                {
                    if (worst == -1 || returnPct[i] < returnPct[worst])
                    {
                        worst = i;
                    }
                }
            }

            return worst;
        }

        // hasPrice matters here in a way it did not for the sums: Capitec's P&L is
        // 0.0 by default, and 0.0 >= 0, so without the test it would count as a winner.
        static int CountWinners()
        {
            int winners = 0;

            for (int i = 0; i < tickers.Length; i++)
            {
                if (hasPrice[i] && pnlZar[i] >= 0)
                {
                    winners++;
                }
            }

            return winners;
        }

        static int CountLosers()
        {
            int losers = 0;

            for (int i = 0; i < tickers.Length; i++)
            {
                if (hasPrice[i] && pnlZar[i] < 0)
                {
                    losers++;
                }
            }

            return losers;
        }

        static int CountValued()
        {
            int valued = 0;

            for (int i = 0; i < tickers.Length; i++)
            {
                if (hasPrice[i])
                {
                    valued++;
                }
            }

            return valued;
        }


        static double[] TotalValuePerCurrency()
        {
            double[] totals = new double[FxCurrencies.Length];

            for (int i = 0; i < tickers.Length; i++)
            {
                if (hasPrice[i])
                {
                    int currencyRow = IndexOf(FxCurrencies, currencies[i]);
                    totals[currencyRow] = totals[currencyRow] + valueZar[i];
                }
            }

            return totals;
        }

        // phase 5

        static double AskForTarget()
        {
            Console.Write("Enter your target amount: ");
            string? typed = Console.ReadLine();
            if (typed == null || typed == "")
            {
                return DefaultTarget;
            }
            double target = double.Parse(typed);
            return target;
}

        static int YearsToTarget(double value, double target, double growthRate = GrowthRate)
        {
            if (growthRate <= 0 || value <= 0)
            {
                return -1;
            }

            int yearsGrown = 0;

            while (value < target)
            {
                value = value * (1 + growthRate);
                yearsGrown++;
            }

            return yearsGrown;
        }

        static int YearsToTargetMaths(double value, double target, double growthRate = GrowthRate)
        {
            if (growthRate <= 0 || value <= 0)
            {
                return -1;
            }

            double years = Math.Ceiling(Math.Log(target / value) / Math.Log(1 + growthRate));

            // Math.Ceiling hands back a double. The compiler will not quietly turn it
            // into an int because that can lose the fractional part, so I say so.
            return (int)years;
        }

        //phase 6

        static string FormatRands(double amount)
        {
            return $"R{amount:N2}";
        }

        static string SkippedTickers()
        {
            string list = "";

            for (int i = 0; i < tickers.Length; i++)
            {
                if (hasPrice[i] == false)
                {
                    if (list == "")
                    {
                        list = tickers[i];
                    }
                    else
                    {
                        list = list + ", " + tickers[i];
                    }
                }
            }

            return list;
        }

        static void WriteValuation(string filename)
        {
            string text = "ticker,name,quantity,price_zar,cost_zar,value_zar,"
                + "pnl_zar,return_pct,weight_pct,flag\n";

            for (int i = 0; i < tickers.Length; i++)
            {
                if (hasPrice[i])
                {
                    text = text
                        + $"{tickers[i]},{names[i]},{quantities[i]},"
                        + $"{priceZar[i]:F2},{costZar[i]:F2},"
                        + $"{valueZar[i]:F2},{pnlZar[i]:F2},"
                        + $"{returnPct[i]:F2},{weightPct[i]:F2},"
                        + $"{flags[i]}\n";
                }
            }

            File.WriteAllText(filename, text);
        }

        static void WriteSummary(double target, int years, string filename)
        {
            double[] currencyTotals = TotalValuePerCurrency();
            int best = BestIndex();
            int worst = WorstIndex();

            string text = "PORTFOLIO VALUATION SUMMARY\n";

            text = text + $"Holdings valued: {CountValued()}\n";
            text = text + $"Holdings skipped: {tickers.Length - CountValued()}\n";

            if (SkippedTickers() != "")
            {
                text = text + $"Skipped tickers: {SkippedTickers()}\n";
            }

            text = text + "\nPORTFOLIO TOTALS\n";
            text = text + $"Total cost: {FormatRands(TotalCost())}\n";
            text = text + $"Total value: {FormatRands(TotalValue())}\n";
            text = text + $"Total P&L: {FormatRands(TotalPnl())}\n";
            text = text + $"Total return: {TotalReturn():F2}%\n";

            text = text + "\nWINNERS AND LOSERS\n";
            text = text + $"Winners: {CountWinners()}\n";
            text = text + $"Losers: {CountLosers()}\n";

            text = text + "\nVALUE BY CURRENCY\n";

            for (int i = 0; i < FxCurrencies.Length; i++)
            {
                text = text + $"{FxCurrencies[i]}: {FormatRands(currencyTotals[i])}\n";
            }

            text = text + "\nPERFORMANCE\n";
            text = text + $"Best performer: {tickers[best]} ({returnPct[best]:F2}%)\n";
            text = text + $"Worst performer: {tickers[worst]} ({returnPct[worst]:F2}%)\n";

            text = text + "\nCONCENTRATION FLAGS\n";

            bool concentratedFound = false;

            for (int i = 0; i < tickers.Length; i++)
            {
                if (flags[i] == "CONCENTRATED")
                {
                    text = text + $"{tickers[i]}: {weightPct[i]:F2}%\n";
                    concentratedFound = true;
                }
            }

            if (concentratedFound == false)
            {
                text = text + "None\n";
            }

            text = text + "\nPROJECTION\n";
            text = text + $"Target: {FormatRands(target)}\n";

            if (years == -1)
            {
                text = text + "Years to target at 8%: never at this growth rate\n";
            }
            else
            {
                text = text + $"Years to target at 8%: {years}\n";
            }

            File.WriteAllText(filename, text);
        }

        static void PrintResults()
        {
            for (int i = 0; i < tickers.Length; i++)
            {
                if (hasPrice[i])
                {
                    Console.WriteLine($"{tickers[i]} {names[i]} qty {quantities[i]} "
                        + $"price_zar {priceZar[i]:F2} cost_zar {costZar[i]:F2} "
                        + $"value_zar {valueZar[i]:F2} pnl_zar {pnlZar[i]:F2} "
                        + $"return_pct {returnPct[i]:F2} weight_pct {weightPct[i]:F2} "
                        + $"{flags[i]}");
                }
            }

            Console.WriteLine($"Skipped tickers: {SkippedTickers()}");
        }

        static void PrintTotals()
        {
            double[] currencyTotals = TotalValuePerCurrency();
            int best = BestIndex();
            int worst = WorstIndex();

            Console.WriteLine($"Total cost: {FormatRands(TotalCost())}");
            Console.WriteLine($"Total value: {FormatRands(TotalValue())}");
            Console.WriteLine($"Total P&L: {FormatRands(TotalPnl())}");
            Console.WriteLine($"Total return: {TotalReturn():F2}%");

            Console.WriteLine($"Best performer: {tickers[best]} ({returnPct[best]:F2}%)");
            Console.WriteLine($"Worst performer: {tickers[worst]} ({returnPct[worst]:F2}%)");
            Console.WriteLine($"Winners: {CountWinners()}  Losers: {CountLosers()}");

            for (int i = 0; i < FxCurrencies.Length; i++)
            {
                Console.WriteLine($"{FxCurrencies[i]} value: {FormatRands(currencyTotals[i])}");
            }
        }

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            Console.WriteLine("Portfolio valuation");

            ReadHoldings("holdings.csv");
            ReadPrices("prices.csv");

            Console.WriteLine($"read {tickers.Length} holdings and {priceTickers.Length} prices");

            ValuePortfolio();
            AddPortfolioWeights();

            PrintResults();
            PrintTotals();

            double target = AskForTarget();

            int years = YearsToTarget(TotalValue(), target);
            int mathsYears = YearsToTargetMaths(TotalValue(), target);

            Console.WriteLine($"Years using loop: {years}");
            Console.WriteLine($"Years using maths: {mathsYears}");
            Console.WriteLine($"Years at 12%: {YearsToTarget(TotalValue(), target, growthRate: 0.12)}");

            WriteValuation("valuation.csv");
            WriteSummary(target, years, "summary.txt");
        }

// DEBUGGER ANSWERS for Phase 3
// I put a conditional breakpoint on the pnl line for AAPL.
//
// quantity = 15
// price = 229.5
// priceInRands = 4096.575
// cost = 44982
// value = 61448.625
//
// After pressing F10 to go to the next line:
// pnl = 16466.625
//
// The price is still in dollars because it is the original price
// from the prices.csv file. priceInRands, cost and value have been
// converted to rands using ToRands.
//
// The values in the debugger also make this clear. The price is 229.5
// and priceInRands is 4096.575, which is roughly 17.85 times bigger.
//
// The types shown in the debugger are:
// quantity = int
// price = double
// cost = double
// value = double
//
// quantity * price gives a double because quantity is an int and
// price is a double. C# converts the int to a double for the calculation.
// This means the decimal part of the result is not lost.

    }
}
