﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("podaj: 1. równanie 2. wartość x 3. minimum zakresu 4. maximum zakresu 5. ilość próbek");

            string all = Console.ReadLine();
            Console.Clear();
            RPN rPN = new RPN(all);
            Console.Read();
        }
    }

    enum typy
    {
        LICZBA,
        ZNAK,
        X,
        WYRAZENIE,
        POTENGA,
        NAWIAS,
        PI
    };

   

    class typ
    {
        public typ(typy t, string v)
        {
            typ_of = t;
            value = v;
        }
        public string value { get; private set; }
        public typy typ_of { get; private set; }
    }

    class EquasionException : Exception
    {
        public EquasionException(string message) : base(message)
        { }
    }

    class RPN
    {
        public RPN(string all)
        {
            try
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == '"')
                        all = all.Remove(i, 1);
                }
                string[] array = all.Split(' ');

                if (array.Length != 5)
                    throw new EquasionException("Nie podałeś odpowiedniej ilości parametrów");

                string r = "";
                List<typ> onp = FindType(array[0].Replace('.', ','), double.NaN, ref r);
                Console.WriteLine(r);

                show_ready(onp);
                calculate_unnown(double.Parse(array[1]), onp);
                calculate_unnown_range(double.Parse(array[2]), double.Parse(array[3]), int.Parse(array[4]), onp);

            }
            catch (EquasionException exe)
            {
                Console.WriteLine(exe.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        List<typ> deepcopy(List<typ> elementy)
        {
            List<typ> temp = new List<typ>();
            for (int j = 0; j < elementy.Count; j++)
            {
                temp.Add(new typ(elementy[j].typ_of, elementy[j].value));
            }
            return temp;
        }

        char[] znaki = { '-', '+', '/', '*', '^' };
        char[] liczby = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', ',' };

        List<typ> FindType(string row, double unnown, ref string r)
        {
            Dictionary<string, int> priority = new Dictionary<string, int>()
            {
                {"-",1},
                {"+",1},
                {"*",2},
                {"/",2},
                {"^",3}
            };
            List<typ> elements = new List<typ>();
            List<Stack<typ>> characters = new List<Stack<typ>>();
            Stack<typ> expresCounts = new Stack<typ>();
            characters.Add(new Stack<typ>());
            int eqCoun = 0, expresCount = 0;
            string number = "", minus = "", temp;
            typ ischaracter = null;
            bool isDiv = false, numIsT = false, eqIsOpen = true,minusnot = false;

            void numbers()
            {
                minus = "";
                isDiv = false;
                numIsT = true;
                eqIsOpen = false;
            }

            void equasions()
            {
                numIsT = false;
                eqIsOpen = true;
                minus = "";
                eqCoun++;
                expresCount++;
                if (characters.Count <= eqCoun)
                {
                    characters.Add(new Stack<typ>());
                }
            }

            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] == '-' && eqIsOpen)
                {
                    if(minusnot)
                        throw new EquasionException("pierwiastek nie może być ujemny");
                    minus = "-";
                    eqIsOpen = false;
                    continue;
                }

                if (row.Length >= i + 2 && row.Substring(i, 2) == "PI")
                {
                    if (numIsT)
                        throw new EquasionException("Coś jest źle z równaniem pi");

                    i += 1;
                    elements.Add(new typ(typy.PI, minus + Math.PI.ToString()));
                    r += minus + "PI";
                    numbers();
                    continue;
                }

                if (row[i] == 'x')
                {
                    if (isDiv && unnown == 0 || numIsT)
                        throw new EquasionException("Coś jest źle z równaniem lub /0");

                    elements.Add(new typ(typy.X, minus + "x"));
                    r += minus + "x ";
                    numbers();
                    continue;
                }

                if (row.IndexOfAny(liczby, i) == i)
                {
                    while (row.IndexOfAny(liczby, i) == i)
                    {
                        number += row[i];
                        i++;
                    }
                    i--;
                    if (isDiv && double.Parse(number) == 0 || numIsT || number == "," || number == ".")
                        throw new EquasionException("Coś jest źle z równaniem  ");

                    elements.Add(new typ(typy.LICZBA, minus + number));
                    r += minus + number + " ";
                    number = "";
                    numbers();
                    continue;
                }

                if (row.IndexOfAny(znaki, i) == i)
                {
                    if (!numIsT || minus == "-" || eqIsOpen)
                        throw new EquasionException("Coś jest źle z równaniem znak");

                    if (row[i] == '^')
                        ischaracter = new typ(typy.POTENGA, row[i].ToString());
                    else
                    {
                        if (row[i].ToString() == "/")
                            isDiv = true;
                        ischaracter = new typ(typy.ZNAK, row[i].ToString());
                    }
                    r += row[i] + " ";
                    typ z = null;

                    int p1, p2;

                    priority.TryGetValue(ischaracter.value, out p1);
                    while (characters[eqCoun].Count > 0)
                    {
                        z = characters[eqCoun].Pop();
                        priority.TryGetValue(z.value, out p2);
                        if (p1 <= p2)
                        {
                            elements.Add(z);
                        }
                        else
                        {
                            characters[eqCoun].Push(z);
                            break;
                        }
                    }
                    characters[eqCoun].Push(ischaracter);
                    numIsT = false;
                    continue;
                }

                if (row.Length >= i + 5)
                {
                    temp = row.Substring(i, 4);
                    if (temp == "abs(" || temp == "cos(" || temp == "sin(" || temp == "tan(" || temp == "exp(" || temp == "log(")
                    {
                        temp = minus + temp.Substring(0, 3);
                        expresCounts.Push(new typ(typy.WYRAZENIE, temp));
                        r += temp + " ( ";
                        i += 3;
                        equasions();
                        continue;
                    }
                    temp = row.Substring(i, 5);
                    if (temp == "sqrt(" || temp == "cosh(" || temp == "sinh(" || temp == "tanh(" || temp == "asin(" || temp == "acos(" || temp == "atan(")
                    {
                        if (temp == "sqrt(")
                            minusnot = true;
                        temp = minus + temp.Substring(0, 4);
                        expresCounts.Push(new typ(typy.WYRAZENIE, temp));
                        r += temp + " ( ";
                        i += 4;
                        equasions();
                        continue;
                    }
                }

                if (row[i] == '(')
                {
                    r += "( ";
                    eqCoun++;
                    if (characters.Count <= eqCoun)
                    {
                        characters.Add(new Stack<typ>());
                    }
                    expresCounts.Push(new typ(typy.NAWIAS, "("));
                    eqIsOpen = true;
                    continue;
                }

                if (row[i] == ')')
                {
                    r += ") ";
                    if (!numIsT || expresCounts.Count <= 0)
                        throw new EquasionException("Coś jest źle z równaniem nawias zamykającym");

                    typ t = expresCounts.Pop();
                    while (characters[eqCoun].Count != 0)
                    {
                        elements.Add(characters[eqCoun].Pop());
                    }
                    eqCoun--;
                    if (t.typ_of == typy.WYRAZENIE)
                    {
                        expresCount--;
                        elements.Add(t);
                    }
                    continue;

                }
                throw new EquasionException("równanie ma niepoprawne znaki w miejscu: " + i);
            }

            if (eqCoun != 0 || !numIsT || isDiv)
            {
                throw new EquasionException("Coś jest źle z równaniem brak nawiasu lub za durza ilość, na końcu +,-,*,/,^");
            }

            while (characters[eqCoun].Count != 0)
            {
                elements.Add(characters[eqCoun].Pop());
            }

            return elements;
        }

        void calculate_unnown(double nie, List<typ> elements)
        {
            List<typ> backup = deepcopy(elements);
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].typ_of == typy.X)
                {
                     if (elements[i].value == "-x")
                            backup[i] = new typ(typy.LICZBA, '-' + nie.ToString());
                        else
                            backup[i] = new typ(typy.LICZBA, nie.ToString());
                }
            }
            Console.WriteLine(Calculate(backup));
        }

        void calculate_unnown_range(double min, double max, int ammount, List<typ> elements)
        {
            List<typ> backup = deepcopy(elements);
            double am = (max - min) / (ammount - 1);
            am = Math.Round(am, 15);
            for (double i = 0; i < ammount; i++)
            {

                for (int j = 0; j < elements.Count; j++)
                {
                    if (elements[j].typ_of == typy.X)
                    {
                         if (elements[j].value == "-x")
                                backup[j] = new typ(typy.LICZBA, '-' + min.ToString());
                            else
                                backup[j] = new typ(typy.LICZBA, min.ToString());
                    }
                }
                
                Console.WriteLine("{0} => {1}", Math.Round(min, 10), Calculate(backup));
                min += am;
            }
        }

        string Calculate(List<typ> elements)
        {
            double typ1, typ2;
            Stack<typ> equasion = new Stack<typ>();

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].typ_of == typy.LICZBA || elements[i].typ_of == typy.X || elements[i].typ_of == typy.PI)
                {
                    equasion.Push(elements[i]);
                }
                else if (elements[i].typ_of == typy.ZNAK)
                {
                    if (equasion.Count() >= 2 && double.TryParse(equasion.Pop().value, out typ1) && double.TryParse(equasion.Pop().value, out typ2))
                        equasion.Push(new typ(typy.LICZBA, calculateS(typ2, typ1, elements[i].value)));
                }
                else if (elements[i].typ_of == typy.WYRAZENIE)
                {
                    if (equasion.Count() >= 1 && double.TryParse(equasion.Pop().value, out typ1))
                        equasion.Push(new typ(typy.LICZBA, calculateexpresCounts(typ1, elements[i].value)));
                }
                else if (elements[i].typ_of == typy.POTENGA)
                {
                    if (equasion.Count() >= 2 && double.TryParse(equasion.Pop().value, out typ1) && double.TryParse(equasion.Pop().value, out typ2))
                        equasion.Push(new typ(typy.LICZBA, Math.Pow(typ2, typ1).ToString()));
                }
            }
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-CA");

            return string.Format("{0:0.0#########################################}", double.Parse(equasion.Pop().value));
        }

        void show_ready(List<typ> elements)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < elements.Count; i++)
                stringBuilder.Append(elements[i].value + " ");

            Console.WriteLine(stringBuilder.ToString());
        }

        char[] liczby2 = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };

        void revers(string onp)
        {
            if (onp != null)
            {
                string[] elementy = onp.Split(' ');
                string typ1, typ2;
                StringBuilder sb = new StringBuilder();

                Stack<string> store = new Stack<string>();

                for (int i = 0; i < elementy.Length; i++)
                {
                    if (elementy[i] == "PI" || elementy[i] == "x" || elementy[i].IndexOfAny(liczby2) == 0 && elementy[i].Length > 0)
                    {
                        store.Push(elementy[i]);
                    }
                    else if (elementy[i].IndexOfAny(znaki) == 0)
                    {
                        typ1 = store.Pop();
                        typ2 = store.Pop();

                        switch (elementy[i])
                        {
                            case "-":
                                store.Push(sb.AppendFormat("({0}-{1})", typ2, typ1).ToString());
                                break;
                            case "+":
                                store.Push(sb.AppendFormat("({0}+{1})", typ2, typ1).ToString());
                                break;
                            case "*":
                                if (typ2.Contains("+") || typ2.Contains("-"))
                                {
                                    typ2 = sb.AppendFormat("({0})", typ2).ToString();
                                    sb.Clear();
                                }
                                if (typ1.Contains("+") || typ1.Contains("-"))
                                {
                                    typ1 = sb.AppendFormat("({0})", typ1).ToString();
                                    sb.Clear();
                                }
                                store.Push(sb.AppendFormat("({0}*{1})", typ2, typ1).ToString());
                                break;
                            case "/":
                                if (typ2.Contains("+") || typ2.Contains("-"))
                                {
                                    typ2 = sb.AppendFormat("({0})", typ2).ToString();
                                    sb.Clear();
                                }
                                if (typ1.Contains("+") || typ1.Contains("-"))
                                {
                                    typ1 = sb.AppendFormat("({0})", typ1).ToString();
                                    sb.Clear();
                                }
                                store.Push(sb.AppendFormat("({0}/{1})", typ2, typ1).ToString());
                                break;
                            case "^":
                                if (typ1.Length >= 3)
                                {
                                    typ1 = sb.AppendFormat("({0})", typ1).ToString();
                                    sb.Clear();
                                }
                                if (typ2.Length >= 3)
                                {
                                    typ2 = sb.AppendFormat("({0})", typ2).ToString();
                                    sb.Clear();
                                }
                                store.Push(sb.AppendFormat("({0}^{1})", typ2, typ1).ToString());
                                sb.Clear();
                                break;
                        }
                        sb.Clear();
                    }
                    else if (elementy[i] != "")
                    {
                        typ1 = store.Pop();
                        switch (elementy[i])
                        {
                            case "cos": store.Push(sb.AppendFormat("cos({0})", typ1).ToString()); break;
                            case "sin": store.Push(sb.AppendFormat("sin({0})", typ1).ToString()); break;
                            case "abs": store.Push(sb.AppendFormat("abs({0})", typ1).ToString()); break;
                            case "tan": store.Push(sb.AppendFormat("tan({0})", typ1).ToString()); break;
                            case "exp": store.Push(sb.AppendFormat("exp({0})", typ1).ToString()); break;
                            case "log": store.Push(sb.AppendFormat("log({0})", typ1).ToString()); break;
                            case "sqrt": store.Push(sb.AppendFormat("sqrt({0})", typ1).ToString()); break;
                            case "cosh": store.Push(sb.AppendFormat("cosh({0})", typ1).ToString()); break;
                            case "sinh": store.Push(sb.AppendFormat("sinh({0})", typ1).ToString()); break;
                            case "tanh": store.Push(sb.AppendFormat("tanh({0})", typ1).ToString()); break;
                            case "asin": store.Push(sb.AppendFormat("asin({0})", typ1).ToString()); break;
                            case "acos": store.Push(sb.AppendFormat("acos({0})", typ1).ToString()); break;
                            case "atan": store.Push(sb.AppendFormat("atan({0})", typ1).ToString()); break;

                        }
                        sb.Clear();
                    }
                }
                Console.WriteLine(store.Pop().Replace(',', '.'));
            }
        }

        string calculateS(double o, double t, string znak)
        {
            switch (znak)
            {
                case "-": return (o - t).ToString();
                case "+": return (o + t).ToString();
                case "*": return (o * t).ToString();
                case "/": if (t == 0) throw new EquasionException("Coś jest źle z równaniem dziel przez 0"); return (o / t).ToString();
            }
            return "";
        }

        string calculateexpresCounts(double o, string w)
        {
            if (w.Contains("-"))
            {
                o *= -1;
                w = w.Substring(1, w.Length - 1);
            }
            switch (w)
            {
                case "cos": return (Math.Cos(o)).ToString();
                case "sin": return (Math.Sin(o)).ToString();
                case "abs": return (Math.Abs(o)).ToString();
                case "tan": return (Math.Tan(o)).ToString();
                case "exp": return (Math.Exp(o)).ToString();
                case "log": return (Math.Log(o)).ToString();
                case "sqrt": if(o<0) throw new EquasionException("pierwiastek nie może być ujemny"); return (Math.Sqrt(o)).ToString();
                case "cosh": return (Math.Cosh(o)).ToString();
                case "sinh": return (Math.Sinh(o)).ToString();
                case "tanh": return (Math.Tanh(o)).ToString();
                case "asin": return (Math.Asin(o)).ToString();
                case "acos": return (Math.Acos(o)).ToString();
                case "atan": return (Math.Atan(o)).ToString();
            }
            return "";
        }
    }

}
