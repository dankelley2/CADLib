using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CAD
{
    public class CadSystem
    {
        //Create commandHistory object
        public CommandHistory commandHistory = new CommandHistory();
        public CadSystem.ClickCache clickCache = new CadSystem.ClickCache();
        public GridSystem gridSystem;
        public ShapeSystem shapeSystem = new ShapeSystem();
        public ShapeSystem.Shape SelectedShape;
        public PointF CurrentPosition = new PointF(0, 0);
        public bool RelativePositioning = false;
        public Dictionary<string, Action<List<float>>> drawingFunctions = new Dictionary<string, Action<List<float>>>();
        public Dictionary<string, Action> gridFunctions = new Dictionary<string, Action>();
        public bool InProcess = false;

        public CadSystem(PointF origin, SizeF containerSize, float scale, int dpi)
        {
            this.gridSystem = new GridSystem(origin, containerSize, scale, dpi);
            this.shapeSystem.SetGrid(gridSystem);
            //SET FUNCTIONS
            this.drawingFunctions.Add("L", ShapeSystem.Line.AddLine);
            //this.drawingFunctions.Add("P", ShapeSystem.Path.AddPath); //not implemented yet
            this.drawingFunctions.Add("C", gridSystem.SetCursor);
            Action PosToggle =
                () => gridSystem.TogglePositioning();
            this.gridFunctions.Add("R", PosToggle);
        }

        public void ParseInput(string text)
        {
            //Split Input
            IO.parsedObj result = IO.parsing.parseString(text, ";, ");

            //Switch first Letter
            if (drawingFunctions.ContainsKey(result.type))
                drawingFunctions[result.type].Invoke(result.value);
            else if (gridFunctions.ContainsKey(result.type))
                gridFunctions[result.type].Invoke();
            else
                Console.WriteLine("Command not recognized");
        }

        public class IO
        {

            public static class parsing
            {
                static char delim = ';';
                public static parsedObj parseString(string input, string delims)
                {
                    foreach (char c in delims)
                    {
                        if (input.IndexOf(c) != -1)
                        {
                            delim = c;
                        }
                    }
                    string[] text_array = input.Split(delim);
                    parsedObj retObj = new parsedObj();

                    if (Regex.IsMatch(text_array[0], @"^[a-zA-Z]+?.*?")) // if starts with a string
                    {
                        retObj.type = text_array[0].ToUpper();
                        if (text_array.Length > 1)
                        {
                            for (int i = 1; i < text_array.Length; i++)
                            {
                                retObj.value.Add(float.Parse(text_array[i], CultureInfo.InvariantCulture.NumberFormat));
                            }
                        }
                    }

                    return retObj;
                }
            }

            public class parsedObj
            {
                public string type = null;
                public List<float> value = new List<float>();
            }
        }

        public class ClickCache
        {
            private Queue<PointF> cache = new Queue<PointF>();
            public PointF dequeue()
            {
                return cache.Dequeue();
            }
            public int enqueue(PointF point)
            {
                cache.Enqueue(point);
                return cache.Count();
            }
            public int count()
            {
                return cache.Count();
            }
            public PointF peek()
            {
                return cache.Peek();
            }
        }
    }
    
}
