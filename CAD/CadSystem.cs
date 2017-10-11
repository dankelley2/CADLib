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
            IO.parsedObj result = IO.parsing.parseString(text);
            if (result == null)
            {
                return;
            }
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
                public static parsedObj parseString(string input, char delim = ' ')
                {
                    parsedObj retObj = new parsedObj();

                    if (Regex.IsMatch(input, @"^[a-zA-Z]+(\s[0-9.]*)*?$")) // if starts with a string
                    {
                        input = Regex.Replace(input, @"\s+", @" ");
                        string[] text_array = input.Trim().Split(delim);
                        retObj.type = text_array[0].ToUpper();
                        if (text_array.Length > 1)
                        {
                            for (int i = 1; i < text_array.Length; i++)
                            {
                                retObj.value.Add(float.Parse(text_array[i], CultureInfo.InvariantCulture.NumberFormat));
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Input was not in correct format. Example: 'L 1.5 1 2 2' to draw a line from (1.5,1) to (2,2) while in global positioning.");
                        return null;
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
