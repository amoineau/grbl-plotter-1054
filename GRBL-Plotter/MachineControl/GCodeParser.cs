﻿/*  GRBL-Plotter. Another GCode sender for GRBL.
    This file is part of the GRBL-Plotter application.
   
    Copyright (C) 2015-2020 Sven Hasemann contact: svenhb@web.de

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

// 2020-01-10 line 113 set x,y,z=null

using System;
using System.Windows.Forms;

namespace GRBL_Plotter
{
    /// <summary>
    /// Hold absolute work coordinate for given linenumber of GCode program
    /// </summary>
    class coordByLine
    {   public int lineNumber;          // line number in fCTBCode
        public int figureNumber;
        public xyPoint actualPos;       // accumulates position
        public double alpha;            // angle between old and this position
        public double distance;         // distance to specific point
        public bool isArc;

        public coordByLine(int line, int figure, xyPoint p, double a, bool isarc)
        { lineNumber = line; figureNumber = figure; actualPos = p; alpha = a; distance = -1; isArc = isarc; }

        public coordByLine(int line, int figure, xyPoint p, double a, double dist)
        { lineNumber = line; figureNumber = figure; actualPos = p; alpha = a; distance = dist; isArc = false; }

        public void calcDistance(xyPoint tmp)
        {   xyPoint delta = new xyPoint(tmp - actualPos);
            distance = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        }
    }

    public struct xyzabcuvwPoint
    {
        public double X, Y, Z, A,B,C,U,V,W;
        public xyzabcuvwPoint(xyzPoint tmp)
        { X = tmp.X; Y = tmp.Y; Z = tmp.Z;  A = tmp.A; B = 0;C = 0;U = 0;V = 0;W = 0; }

        public static explicit operator xyPoint(xyzabcuvwPoint tmp)
        { return new xyPoint(tmp.X,tmp.Y); }
        public static explicit operator xyArcPoint(xyzabcuvwPoint tmp)
        { return new xyArcPoint(tmp.X, tmp.Y,0 ,0 ,0); }
    }

    /// <summary>
    /// Hold parsed GCode line and absolute work coordinate for given linenumber of GCode program
    /// </summary>
    class gcodeByLine
    {   // ModalGroups
        public int lineNumber;          // line number in fCTBCode
        public int figureNumber;
        public string codeLine;         // copy of original gcode line
        public byte motionMode;         // G0,1,2,3
        public bool isdistanceModeG90;  // G90,91
        public bool ismachineCoordG53;  // don't apply transform to machine coordinates
        public bool isSubroutine;
        public bool isSetCoordinateSystem;  // don't process x,y,z if set coordinate system

        public byte spindleState;       // M3,4,5
        public byte coolantState;       // M7,8,9
        public int spindleSpeed;        // actual spindle spped
        public int feedRate;            // actual feed rate
        public double? x, y, z, a, b, c, u, v, w, i, j; // current parameters
        public xyzabcuvwPoint actualPos;      // accumulates position
        public double alpha;            // angle between old and this position
        public double distance;         // distance to specific point
        public string otherCode;
        public string info;

        // Trace, Debug, Info, Warn, Error, Fatal
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public gcodeByLine()
        {   resetAll(); }
        public gcodeByLine(gcodeByLine tmp)
        {
            resetAll();
            lineNumber = tmp.lineNumber; figureNumber = tmp.figureNumber; codeLine = tmp.codeLine;
            motionMode = tmp.motionMode; isdistanceModeG90 = tmp.isdistanceModeG90; ismachineCoordG53 = tmp.ismachineCoordG53;
            isSubroutine = tmp.isSubroutine; spindleState = tmp.spindleState; coolantState = tmp.coolantState;
            spindleSpeed = tmp.spindleSpeed; feedRate = tmp.feedRate;
            x = tmp.x; y = tmp.y; z = tmp.z; i = tmp.i; j = tmp.j; a = tmp.a; b = tmp.b; c = tmp.c; u = tmp.u; v = tmp.v; w = tmp.w;
            actualPos = tmp.actualPos; distance = tmp.distance; alpha = tmp.alpha;
            isSetCoordinateSystem = tmp.isSetCoordinateSystem;  otherCode = tmp.otherCode;
        }

        public string listData()
        { return string.Format("{0} mode {1} figure {2}\r", lineNumber, motionMode, figureNumber); }

        /// <summary>
        /// Reset coordinates and set G90, M5, M9
        /// </summary>
        public void resetAll()
        {
            lineNumber = 0; figureNumber = 0; codeLine = "";
            motionMode = 0; isdistanceModeG90 = true; ismachineCoordG53 = false; isSubroutine = false;
            isSetCoordinateSystem = false; spindleState = 5; coolantState = 9; spindleSpeed = 0; feedRate = 0;

            actualPos.X = 0; actualPos.Y = 0; actualPos.Z = 0; actualPos.A = 0; actualPos.B = 0; actualPos.C = 0;
            actualPos.U = 0; actualPos.V = 0; actualPos.W = 0;
            distance = -1; otherCode = ""; info = ""; alpha = 0;

            x = y = z = a = b = c = u = v = w = i = j = null;
                 
            resetCoordinates();
        }
        public void resetAll(xyzPoint tmp)
        {   resetAll();
            actualPos = new xyzabcuvwPoint( tmp);
        }
        /// <summary>
        /// Reset coordinates
        /// </summary>
        public void resetCoordinates()
        {   x = null; y = null; z = null; a = null; b = null; c = null; u = null; v = null; w = null; i = null; j = null;
        }
        public void presetParsing(int lineNr, string line)
        {   resetCoordinates();
            ismachineCoordG53 = false; isSubroutine = false;
            otherCode = "";
            lineNumber = lineNr;
            codeLine = line;
        }

        /// <summary>
        /// parse gcode line
        /// </summary>
        public void parseLine(int lineNr, string line, ref modalGroup modalState)
        {
            presetParsing(lineNr,line);
            char cmd = '\0';
            string num = "";
            bool comment = false;
            double value = 0;
            line = line.ToUpper().Trim();   // 2020-07-26
            isSetCoordinateSystem = false;
            #region parse
            if ((!(line.StartsWith("$") || line.StartsWith("("))) && (line.Length > 1))//do not parse grbl comments
            {
                try
                {
                    foreach (char c in line)
                    {
                        if (c == ';')                                   // comment?
                            break;
                        if (c == '(')                                   // comment starts
                        {   comment = true;  }
                        if (!comment)
                        {
                            if (Char.IsLetter(c))                       // if char is letter
                            {
                                if (cmd != '\0')                        // and command is set
                                {
                                    if (double.TryParse(num, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out value))
                                        parseGCodeToken(cmd, value, ref modalState);
                                }
                                cmd = c;                                // char is a command
                                num = "";
                            }
                            else if (Char.IsNumber(c) || c == '.' || c == '-')  // char is not letter but number
                            {
                                num += c;
                            }
                        }

                        if (c == ')')                                   // comment ends
                        {   comment = false;  }
                    }
                    if (cmd != '\0')                                    // finally after for-each process final command and number
                    {
                        if (double.TryParse(num, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out value))
                            parseGCodeToken(cmd, value, ref modalState);
                    }
                }
                catch (Exception er) { Logger.Error(er, "parseLine"); }
            }
            #endregion
            if (isSetCoordinateSystem)
                resetCoordinates();
        }

        /// <summary>
        /// fill current gcode line structure
        /// </summary>
        private void parseGCodeToken(char cmd, double value, ref modalGroup modalState)
        {
            switch (Char.ToUpper(cmd))
            {
                case 'X':
                    x = value;
                    break;
                case 'Y':
                    y = value;
                    break;
                case 'Z':
                    z = value;
                    break;
                case 'A':
                    a = value;
                    break;
                case 'B':
                    b = value;
                    break;
                case 'C':
                    c = value;
                    break;
                case 'U':
                    u = value;
                    break;
                case 'V':
                    v = value;
                    break;
                case 'W':
                    w = value;
                    break;
                case 'I':
                    i = value;
                    break;
                case 'J':
                    j = value;
                    break;
                case 'F':
                    modalState.feedRate = feedRate = (int)value;
                    break;
                case 'S':
                    modalState.spindleSpeed = spindleSpeed = (int)value;
                    break;
                case 'G':
                    if (value <= 3)                                 // Motion Mode 0-3 c
                    {   modalState.motionMode = motionMode = (byte)value;
                        if (value >= 2)
                            modalState.containsG2G3 = true;
                    }
                    else
                    {   otherCode += "G"+((int)value).ToString()+" ";
                    }

                    if (value == 10)
                    { isSetCoordinateSystem = true; }

                    else if ((value == 20) || (value == 21))             // Units Mode
                    { modalState.unitsMode = (byte)value; }

                    else if (value == 53)                                // move in machine coord.
                    { ismachineCoordG53 = true; }

                    else if ((value >= 54) && (value <= 59))             // Coordinate System Select
                    { modalState.coordinateSystem = (byte)value; }

                    else if (value == 90)                                // Distance Mode
                    { modalState.distanceMode = (byte)value; modalState.isdistanceModeG90 = true; }
                    else if (value == 91)
                    { modalState.distanceMode = (byte)value; modalState.isdistanceModeG90 = false;
                      modalState.containsG91 = true;
                    }
                    else if ((value == 93) || (value == 94))             // Feed Rate Mode
                    { modalState.feedRateMode = (byte)value; }
                    break;
                case 'M':
                    if ((value < 3) || (value == 30))                   // Program Mode 0, 1 ,2 ,30
                    { modalState.programMode = (byte)value; }
                    else if (value >= 3 && value <= 5)                   // Spindle State
                    {   modalState.spindleState = spindleState = (byte)value; }
                    else if (value >= 7 && value <= 9)                   // Coolant State
                    {   modalState.coolantState = coolantState = (byte)value;                    }
                    modalState.mWord = (byte)value;
                    if ((value < 3) || (value > 9))
                        otherCode += "M" + ((int)value).ToString() + " ";
                    break;
                case 'T':
                    modalState.tool = (byte)value;
                    otherCode += "T" + ((int)value).ToString() + " ";
                    break;
                case 'P':
                    modalState.pWord = (int)value;
                    otherCode += "P" + value.ToString() + " ";
                    break;
                case 'O':
                    modalState.oWord = (int)value;
                    break;
                case 'L':
                    modalState.lWord = (int)value;
                    break;
            }
            isdistanceModeG90 = modalState.isdistanceModeG90;
        }
    };

    class modalGroup
    {
        public byte motionMode;           // G0, G1, G2, G3, //G38.2, G38.3, G38.4, G38.5, G80
        public byte coordinateSystem;     // G54, G55, G56, G57, G58, G59
        public byte planeSelect;          // G17, G18, G19
        public byte distanceMode;         // G90, G91
        public byte feedRateMode;         // G93, G94
        public byte unitsMode;            // G20, G21
        public byte programMode;          // M0, M1, M2, M30
        public byte spindleState;         // M3, M4, M5
        public byte coolantState;         // M7, M8, M9
        public byte tool;                 // T
        public int spindleSpeed;          // S
        public int feedRate;              // F
        public int mWord;
        public int pWord;
        public int oWord;
        public int lWord;
        public bool containsG2G3;
        public bool ismachineCoordG53;
        public bool isdistanceModeG90;
        public bool containsG91;

        public modalGroup()     // reset state
        { reset(); }

        public void reset()
        {   motionMode = 0;             // G0, G1, G2, G3, G38.2, G38.3, G38.4, G38.5, G80
            coordinateSystem = 54;      // G54, G55, G56, G57, G58, G59
            planeSelect = 17;           // G17, G18, G19
            distanceMode = 90;          // G90, G91
            feedRateMode = 94;          // G93, G94
            unitsMode = 21;             // G20, G21
            programMode = 0;            // M0, M1, M2, M30
            spindleState = 5;           // M3, M4, M5
            coolantState = 9;           // M7, M8, M9
            tool = 0;                   // T
            spindleSpeed = 0;           // S
            feedRate = 0;               // F
            mWord = 0;
            pWord = 0;
            oWord = 0;
            lWord = 1;
            containsG2G3 = false;
            ismachineCoordG53 = false;
            isdistanceModeG90 = true;
            containsG91 = false;
        }
        public void resetSubroutine()
        {   mWord = 0;
            pWord = 0;
            oWord = 0;
            lWord = 1;
        }
    }

    struct ArcProperties
    {   public double angleStart, angleEnd, angleDiff, radius;
        public xyPoint center;
    };

    class gcodeMath
    {   private static double precision = 0.00001;

        public static bool isEqual(System.Windows.Point a, System.Windows.Point b)
        {   return ((Math.Abs(a.X - b.X) < precision) && (Math.Abs(a.Y - b.Y) < precision));    }
        public static bool isEqual(xyPoint a, xyPoint b)
        {   return ((Math.Abs(a.X - b.X) < precision) && (Math.Abs(a.Y - b.Y) < precision));    }

        public static double distancePointToPoint(System.Windows.Point a, System.Windows.Point b)
        {   return Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));        }

        public static ArcProperties getArcMoveProperties(System.Windows.Point pOld, System.Windows.Point pNew, System.Windows.Point centerIJ, bool isG2)
        { return getArcMoveProperties(new xyPoint(pOld), new xyPoint(pNew), centerIJ.X, centerIJ.Y, isG2); }
        public static ArcProperties getArcMoveProperties(xyPoint pOld, xyPoint pNew, xyPoint center, bool isG2)
        {   return getArcMoveProperties(pOld, pNew, pOld.X - center.X, pOld.Y - center.Y, isG2);}

        public static ArcProperties getArcMoveProperties(xyPoint pOld, xyPoint pNew, double? I, double? J, bool isG2)
        {
            ArcProperties tmp = getArcMoveAngle(pOld, pNew, I, J);
            if (!isG2) { tmp.angleDiff = Math.Abs(tmp.angleEnd - tmp.angleStart + 2 * Math.PI); }
            if (tmp.angleDiff > (2 * Math.PI)) { tmp.angleDiff -= (2 * Math.PI); }
            if (tmp.angleDiff < (-2 * Math.PI)) { tmp.angleDiff += (2 * Math.PI); }

            if ((pOld.X == pNew.X) && (pOld.Y == pNew.Y))
            {   if (isG2) { tmp.angleDiff = -2 * Math.PI; }
                else { tmp.angleDiff = 2 * Math.PI; }
            }
            return tmp;
        }

        public static ArcProperties getArcMoveAngle(xyPoint pOld, xyPoint pNew, double? I, double? J)
        {
            ArcProperties tmp;
			if (I==null) {I=0;}
			if (J==null) {J=0;}
            double i = (double)I;
            double j = (double)J;
            tmp.radius = Math.Sqrt(i * i + j * j);  // get radius of circle
            tmp.center.X = pOld.X + i;
            tmp.center.Y = pOld.Y + j;
            tmp.angleStart = tmp.angleEnd = tmp.angleDiff = 0;
            if (tmp.radius == 0)
                return tmp;

            double cos1 = i / tmp.radius;
            if (cos1 > 1) cos1 = 1;
            if (cos1 < -1) cos1 = -1;
            tmp.angleStart = Math.PI - Math.Acos(cos1);
            if (j > 0) { tmp.angleStart = -tmp.angleStart; }

            double cos2 = (tmp.center.X - pNew.X) / tmp.radius;
            if (cos2 > 1) cos2 = 1;
            if (cos2 < -1) cos2 = -1;
            tmp.angleEnd = Math.PI - Math.Acos(cos2);
            if ((tmp.center.Y - pNew.Y) > 0) { tmp.angleEnd = -tmp.angleEnd; }

            tmp.angleDiff = tmp.angleEnd - tmp.angleStart - 2 * Math.PI;
            return tmp;
        }

        public static double getAlpha(System.Windows.Point pOld, double P2x, double P2y)
        { return getAlpha(pOld.X, pOld.Y, P2x, P2y); }
        public static double getAlpha(System.Windows.Point pOld, System.Windows.Point pNew)
        { return getAlpha(pOld.X, pOld.Y, pNew.X, pNew.Y); }
        public static double getAlpha(xyPoint pOld, xyPoint pNew)
        {   return getAlpha(pOld.X, pOld.Y, pNew.X, pNew.Y); }
        public static double getAlpha(double P1x, double P1y, double P2x, double P2y)
        {
            double s = 1, a = 0;
            double dx = P2x - P1x;
            double dy = P2y - P1y;
            if (dx == 0)
            {
                if (dy > 0)
                    a = Math.PI / 2;
                else
                    a = 3 * Math.PI / 2;
                if (dy == 0)
                    return 0;
            }
            else if (dy == 0)
            {
                if (dx > 0)
                    a = 0;
                else
                    a = Math.PI;
                if (dx == 0)
                    return 0;
            }
            else
            {
                s = dy / dx;
                a = Math.Atan(s);
                if (dx < 0)
                    a += Math.PI;
            }
            return a;
        }

        public static double cutAngle=0, cutAngleLast=0, angleOffset = 0;
        public static void resetAngles()
        {   angleOffset = cutAngle = cutAngleLast = 0.0;  }
        public static double getAngle(System.Windows.Point a, System.Windows.Point b, double offset, int dir)
        {   return monitorAngle(getAlpha(a, b) + offset, dir);  }
        private static double monitorAngle(double angle, int direction)		// take care of G2 cw G3 ccw direction
        {   double diff = angle - cutAngleLast + angleOffset;
            if (direction == 2)
            { if (diff > 0) { angleOffset -= 2 * Math.PI; } }    // clock wise, more negative
            else if (direction == 3)
            {   if (diff < 0) { angleOffset += 2 * Math.PI; } }    // counter clock wise, more positive
            else
            {   if (diff > Math.PI)
                    angleOffset -= 2 * Math.PI;
                if (diff < -Math.PI)
                    angleOffset += 2 * Math.PI;
            }
            angle += angleOffset;
            return angle;
        }
    }
}
