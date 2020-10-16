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
/*  GCodeVisuAndTransform.cs
    Scaling, Rotation, Remove OffsetXY, Mirror X or Y
    During transformation the drawing path will be generated, because cooridantes are already parsed.
    Return transformed GCode 
*/
/* 2020-01-13 convert GCodeVisuAndTransform to a static class
 * 2020-02-18 extend simulation for tangetial angle
 * 2020-03-09 bug-fix simulation of G2/3 code without tangential line 525
 * 2020-04-04 bug-fix simulation of G2/3 code without tangential line 518 - never ending rotation
 * 2020-07-22 show ProcessedPath
 * 2020-07-25 bug fix in simulaation -> only addArc if r > 0
 * 2020-08-13 bug fix transformGCodeMirror with G91 
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GRBL_Plotter
{
    public static partial class VisuGCode
    {
        public static xyPoint getCenterOfMarkedFigure()
        {   RectangleF selectionBounds = pathMarkSelection.GetBounds();
            float centerX = selectionBounds.X + selectionBounds.Width / 2;
            float centerY = selectionBounds.Y + selectionBounds.Height / 2;
            return new xyPoint((double)centerX, (double)centerY); 
        }
        public static double getWidthOfMarkedFigure()
        {   RectangleF selectionBounds = pathMarkSelection.GetBounds();
            return (double)selectionBounds.Width;
        }
        public static double getHeightOfMarkedFigure()
        {   RectangleF selectionBounds = pathMarkSelection.GetBounds();
            return (double)selectionBounds.Height;
        }
        private static bool xyMove(gcodeByLine tmp)
        { return ((tmp.x != null) || (tmp.y != null)); }

        private static bool sameXYPos(gcodeByLine tmp1, gcodeByLine tmp2)
        { return ((tmp1.x == tmp2.x) && (tmp1.y == tmp2.y) && (tmp1.z == tmp2.z));
        }

        /// <summary>
        /// mirror gcode
        /// </summary>
        public static string transformGCodeMirror(translate shiftToZero = translate.MirrorX)
        {
            Logger.Debug("..transformGCodeMirror {0}", shiftToZero);
            if (gcodeList == null) return "";

            xyPoint centerOfFigure = xyzSize.getCenter();		// center of whole graphics
            if (lastFigureNumber > 0)
                centerOfFigure = getCenterOfMarkedFigure();		// center of selected figure

            oldLine.resetAll(grbl.posWork);         			// reset coordinates and parser modes
            clearDrawingnPath();                    			// reset path, dimensions

            bool offsetApplied = false;
			double lastAbsPos = 0;
			
            foreach (gcodeByLine gcline in gcodeList)
            {
                if ((lastFigureNumber > 0) && (gcline.figureNumber != lastFigureNumber))	// process all graphics or selected figure
                { 	if (!gcline.isdistanceModeG90 && offsetApplied)			// correct relative movement of next figure
					{	if ((gcline.motionMode == 0) && ((gcline.x != null) || (gcline.y != null)))
                        {	if (shiftToZero == translate.MirrorX)
							{	gcline.x = gcline.actualPos.X - lastAbsPos;	}
							if (shiftToZero == translate.MirrorY)
							{	gcline.y = gcline.actualPos.Y - lastAbsPos;	}
							offsetApplied = false;
						}
					}				
					continue; 
				}

                if (!gcline.ismachineCoordG53)
                {
                    // switch circle direction
                    if ((shiftToZero == translate.MirrorX) || (shiftToZero == translate.MirrorY))	// mirror xy 
                    {
                        if (gcline.motionMode == 2) { gcline.motionMode = 3; }
                        else if (gcline.motionMode == 3) { gcline.motionMode = 2; }
                    }
                    if (shiftToZero == translate.MirrorX)           // mirror x
                    {
                        if (gcline.x != null)
                        {
							if (gcline.isdistanceModeG90)
								gcline.x = -gcline.x + 2 * centerOfFigure.X;
							else
							{	gcline.x = -gcline.x;
							    if ((gcline.motionMode == 0) && !offsetApplied) 	// at relative movement, apply offset only once on G0
								{	gcline.x = -gcline.x + 2*(centerOfFigure.X - gcline.actualPos.X);
									offsetApplied = true;
								}
								lastAbsPos = -gcline.actualPos.X + 2 * centerOfFigure.X;
                            }						
                        }
                        gcline.i = -gcline.i;
                    }
                    if (shiftToZero == translate.MirrorY)           // mirror y
                    {
                        if (gcline.y != null)
                        {
							if (gcline.isdistanceModeG90)
								gcline.y = -gcline.y + 2 * centerOfFigure.Y;
							else
							{	gcline.y = -gcline.y;
							    if ((gcline.motionMode == 0) && !offsetApplied) 
								{	gcline.y = -gcline.y + 2*(centerOfFigure.Y - centerOfFigure.Y);
									offsetApplied = true;
								}
								lastAbsPos = -gcline.actualPos.Y + 2 * centerOfFigure.Y;
                            }
                        }
                        gcline.j = -gcline.j;
                    }
                    if (shiftToZero == translate.MirrorRotary)           // mirror rotary
                    {
                        string rotary = Properties.Settings.Default.ctrl4thName;
                        if ((rotary == "A") && (gcline.a != null)) { gcline.a = -gcline.a; }
                        else if ((rotary == "B") && (gcline.b != null)) { gcline.b = -gcline.b; }
                        else if ((rotary == "C") && (gcline.c != null)) { gcline.c = -gcline.c; }
                    }

                    calcAbsPosition(gcline, oldLine);
                    oldLine = new gcodeByLine(gcline);   // get copy of newLine
                }
            }
            return createGCodeProg();
        }

        public static string cutOutFigure()
        {
            if (lastFigureNumber > 0)
            {   for (int i = gcodeList.Count - 1; i >= 0; i--)
                {   if ((gcodeList[i].figureNumber != -1) && (gcodeList[i].figureNumber != lastFigureNumber))
                    { gcodeList.RemoveAt(i); }
                }
            }
            return createGCodeProg();
        }

        public static string transformGCodeRadiusCorrection(double radius)
        {
            Logger.Debug("..transformGCodeRadiusCorrection r: {0}", radius);
            if (gcodeList == null) return "";

            if (lastFigureNumber > 0)
            { pathBackground = (GraphicsPath)pathMarkSelection.Clone();
                origWCOLandMark = (xyPoint)grbl.posWCO;
            }
            else
            { pathBackground = (GraphicsPath)pathPenDown.Clone();
                origWCOLandMark = (xyPoint)grbl.posWCO;
            }

            xyPoint[] offset = new xyPoint[4];
            int i, figureStart, figure2nd, prev, act, next;
            int counter = 0, isFirst = 0;
            bool figureProcessed = false;
            bool closeFigure = false;
            bool endFigure = false;

            figure2nd = figureStart = prev = act = next = 0;
            int offType = 0;

            for (i = 1; i < gcodeList.Count; i++)
            {
                if ((i == (gcodeList.Count - 1)) || ((lastFigureNumber > 0) && (gcodeList[i].figureNumber != lastFigureNumber)))  // if wrong selection, nothing to do
                { if (figureProcessed)        // correct last point
                    {   figureProcessed = false;
                        goto ProcessesPath;
                    }
                    continue;
                }

                if (gcodeList[i].ismachineCoordG53)     // machine coordinates, do not change
                { continue; }

                if (!xyMove(gcodeList[i]))              // no xy moves, nothing to do - except G0 Z
                { continue; }

                while ((gcodeList[i].codeLine == gcodeList[i + 1].codeLine) && (i < gcodeList.Count))  // remove double lines (lff font)
                { gcodeList.RemoveAt(i + 1); }

                while (sameXYPos(gcodeList[i], gcodeList[i + 1]) && (i < (gcodeList.Count - 1)))  // remove double coordinates
                { gcodeList.RemoveAt(i + 1); }

                Logger.Trace("   ----- {0} -----",i);

                if (gcodeList[i].motionMode > 1)
                {   double tmpR = Math.Sqrt((double)gcodeList[i].i * (double)gcodeList[i].i + (double)gcodeList[i].j * (double)gcodeList[i].j);
                    bool remove = false;
                    double abs_radius = Math.Abs(radius);
                    if (gcodeList[i].motionMode == 2)
                    {   if ((radius < 0) && (tmpR < abs_radius)) remove = true;
                    }
                    if (gcodeList[i].motionMode == 3)
                    {   if ((radius > 0) && (tmpR < abs_radius)) remove = true;
                    }
                    if (remove)
                    {   gcodeList[i].i = null; gcodeList[i].j = null;
                        gcodeList[i].motionMode = 1;
                        Logger.Trace("   Radius too small, do G1 {0}",gcodeList[act].codeLine);
                    }
                }
                figureProcessed = true;                 // must stay before jump label
                next = i;
                endFigure = false;

 ProcessesPath:
 //               gcodeList[i].info += " "+i.ToString()+" "+ figureProcessed.ToString()+" "+lastFigureNumber.ToString();
                if (counter == 0)
                {   figureStart = prev = act = next; }  // preset indices

                if ((gcodeList[prev].motionMode == 0) && (gcodeList[act].motionMode >= 1))  // find start of figure
                {   figureStart = prev; figure2nd = act; isFirst = 0; }//gcodeList[prev].info += " #start "; }

                if ((gcodeList[act].motionMode >= 1) && (gcodeList[next].motionMode == 0))  // find end of figure
                { endFigure = true; figureProcessed = false; }// gcodeList[act].info += " #end ";            }

                closeFigure = false;
                if (act != prev)
                {
                    xyArcPoint p1 = fillPointData(prev, prev);
                    xyArcPoint p2 = fillPointData(prev, act);

                    if ((gcodeList[act].actualPos.X == gcodeList[figureStart].actualPos.X) && (gcodeList[act].actualPos.Y == gcodeList[figureStart].actualPos.Y))
                    { next = figure2nd; closeFigure = true; }//                    gcodeList[act].info += " closefig "; }

                    Logger.Trace("   {0}",gcodeList[act].codeLine);

                    offType = createOffsetedPath((isFirst++ == 0), (endFigure && !closeFigure), figureStart, prev, act, next, radius, ref offset);

                    Logger.Trace(" typ {0} {1} {2} {3} {4} {5} ", offType, endFigure, figureStart, prev, act, next);

                    if (closeFigure)// && !endFigure)
                    {

                        Logger.Trace(" close Figure {0:0.00} {1:0.00}  ", offset[2].X, offset[2].Y);

                        gcodeList[figureStart].x = offset[2].X; gcodeList[figureStart].y = offset[2].Y;    // close figure
                        if (gcodeList[figure2nd].motionMode > 1)    // act
                        {
                            bool isFullCircle = ((p1.X == p2.X) && (p1.Y == p2.Y));
                            if (!isFullCircle)
                            {
                                xyArcPoint p3 = fillPointData(prev, act); //fillPointData(act, next);
                                gcodeList[figure2nd].i = p3.CX - offset[2].X;
                                gcodeList[figure2nd].j = p3.CY - offset[2].Y;   // offset radius

                                Logger.Trace(" correct Arc center of f2nd {0} origX {1:0.00} origY {2:0.00}  ", figure2nd, p3.CX, p3.CY);

                            }
                        }
                    }
                    next = i;   // restore next
                    if (offType >= 1)           // arc or line was inserted to connect points
                    {   act++; next++; counter++; i++; }    // inc. counters
                }

                prev = act; act = next; counter++;

                if (endFigure)
                {   prev = act; figureStart = act = i; }  // preset indices

                if (closeFigure)
                { isFirst = 0; }
            }
            return createGCodeProg();
        }

        private static bool sameSign(double? a, double? b)
        {   if (double.IsNaN((double)a) || double.IsNaN((double)b))
                return false;
            if ((a.HasValue && b.HasValue))
                return (Math.Sign((double)a) == Math.Sign((double)b));
            else
                return false;                   
        }

        // calculate and apply offset for given coordinates in gcodeList[prev,act,next] (which should have xy moves)
        public static int createOffsetedPath(bool isFirst, bool isEnd, int iInitial, int prev, int act, int next, double radius, ref xyPoint[] offset)
        {
            xyArcPoint p1 = fillPointData(prev, prev);
            xyArcPoint p2 = fillPointData(prev, act);
            xyArcPoint p3 = fillPointData(act, next);

            bool isArc = (gcodeList[act].motionMode > 1);
            bool isFullCircle = ((p1.X==p2.X)&&(p1.Y==p2.Y));
            int offsetType = crc.getPointOffsets(ref offset, p1, p2, p3, radius, isEnd);

            Logger.Trace(" offset typ{0} x{1:0.000} y{2:0.000}", offsetType, offset[1].X, offset[1].Y);

     /*       if (offsetType == -2)   // intersection not successfull
            {
                gcodeList[act].motionMode = 1; gcodeList[act].x = null; gcodeList[act].y = null;    // clear move
                p2 = p3; p3 = fillPointData(next, next+1);      // next+1 not save
                offsetType = crc.getPointOffsets(ref offset, p1, p2, p3, radius, isEnd);
#if (debuginfo)
                log.Add(string.Format(" redo offset x{0:0.000} y{1:0.000}", offset[1].X, offset[1].Y));
#endif
            }*/

            if (isArc && !isFullCircle)   // replace arc by line, if start and end-point are too close
            {   double dist = offset[0].DistanceTo(offset[1]);
                double a1 = offset[0].AngleTo(offset[1]);
                double a2 = ((xyPoint)p1).AngleTo((xyPoint)p2);
                if (dist < 0.1)
                {   gcodeList[act].motionMode = 1;
                    gcodeList[act].i = null;
                    gcodeList[act].j = null;
                }
            }

            if (isFirst)                   // offset 1st point
            {   gcodeList[iInitial].x = offset[0].X; gcodeList[iInitial].y = offset[0].Y;    // offset 1st point
            }

            gcodeList[act].x = offset[1].X; gcodeList[act].y = offset[1].Y;         // offset point

            Logger.Trace(" createOffsetedPath Offset x{0:0.000} y{1:0.000}", gcodeList[act].x, gcodeList[act].y);

            if (isArc)                                                              // offset radius     
            {   double iNew = p2.CX - (double)gcodeList[prev].x;
                double jNew = p2.CY - (double)gcodeList[prev].y; ;
                gcodeList[act].i = iNew; gcodeList[act].j = jNew;      // offset radius    

                if (!sameSign(gcodeList[act].i,iNew) || !sameSign(gcodeList[act].j, jNew)) // radius now negative
                { }

    //            if ((gcodeList[act].i == 0) && (gcodeList[act].j == 0)) // radius = 0, command not needed
   //             {   gcodeList[act].motionMode = 1; gcodeList[act].i = null; gcodeList[act].j = null;}
            }

            if (offsetType >= 1)     // insert arc to connect lines
            {   int insert = act + 1;
                gcodeList.Insert(insert, new gcodeByLine(gcodeList[act]));             // insert a copy of actual move
                gcodeList[insert].x = offset[2].X; gcodeList[insert].y = offset[2].Y;   // set end-pos.

                double dist = offset[1].DistanceTo(offset[2]);                          // if distance great enough use arc
                if (dist > 0.1)      // make arc, if start and end-point are not too close
                {   gcodeList[insert].motionMode = (byte)((radius > 0) ? 2 : 3);
                    gcodeList[insert].i = gcodeList[insert].actualPos.X - offset[1].X;
                    gcodeList[insert].j = gcodeList[insert].actualPos.Y - offset[1].Y;
                }
                else
                    gcodeList[insert].motionMode = 1;

                Logger.Trace(" createOffsetedPath Insert G{0} x{1:0.000} y{2:0.000}", gcodeList[insert].motionMode, gcodeList[insert].x, gcodeList[insert].y);

            }
            return offsetType;
        }

        private static xyArcPoint fillPointData(int prevLine, int tmpLine)
        {
            xyArcPoint tmpPoint;
            tmpPoint = (xyArcPoint)gcodeList[tmpLine].actualPos;
            if ((tmpPoint.mode = gcodeList[tmpLine].motionMode) > 1)
            { tmpPoint.CX = gcodeList[prevLine].actualPos.X + (double)gcodeList[tmpLine].i; tmpPoint.CY = gcodeList[prevLine].actualPos.Y + (double)gcodeList[tmpLine].j; }
            return tmpPoint;
        }

        /* 2020-08-04 implemented via graphicRelated ReDoReversePath
        public static string transformGCodeReversePath()
        {
            Logger.Debug("..transformGCodeReversePath figure {0}", lastFigureNumber);
            if (gcodeList == null) return "";

            List<gcodeByLine> tmpCode = new List<gcodeByLine>();
            if (xmlMarker.GetFigureByFigureNr(lastFigureNumber))
            {
                for (int i = xmlMarker.tmpFigure.lineEnd; i >= xmlMarker.tmpFigure.lineEnd; i--)
                { }

            }

            return createGCodeProg();
        }
        */

        // calculate intermediate steps betwee code lines
        public static class Simulation
        {
            private static int lineNr = 0;  // actual code line
            public static int dt = 50;    // step width
            private static xyPoint posXY = new xyPoint();
            private static double posZ = 0;
            private static double posA = 0;
            private static double posAngle = 0;
            private static float feedRate = 1000;
            private static float feedRateRapid = 2000;
            private static double stepWidth = 10;
            private static double remainingStep = 10;
            private static bool isIntermediate = false;
            private static double distance  = 0;
            private static gcodeByLine codeLast = new gcodeByLine();
            private static gcodeByLine codeNext = new gcodeByLine();
            private static bool isTangentialZ = false;
            private static bool isPenDownOld = false;
            private static bool isPenDownNow = false;
            private static bool isG2G3 = false;
            private static float drawAngleOld = 0;
            private static float drawAngleNow = 0;
            public static GraphicsPath pathSimulation = new GraphicsPath();

            private struct length
			{	public double XY;
                public double Z;
                public double A;
                public double Arc;
                public double Max;
            };
            private static length diff;

            public static void Reset()
            {   lineNr = 0;  isIntermediate = false;
                diff.XY = diff.Z = diff.A = diff.Arc = 0;
                distance = 0; dt = 50; posZ = 0; posA = 0;
                remainingStep = stepWidth = 10;
                lastPosMarker = posXY = grbl.posMarker = new xyPoint();
                posAngle = grbl.posMarkerAngle = 0;
                codeNext = new gcodeByLine(simuList[lineNr]);
                createMarkerPath();
                isTangentialZ = (tangentialAxisName == "Z");
                pathSimulation.Reset();
                markSelectedFigure(-1);
                isPenDownOld = isPenDownNow = false;
            }
            public static void setDt(int tmp)
            { dt = tmp; }
            public static double getZ()
            { return posZ; }
            public static double getA()
            { return posA; }
            public static int Next()
            {   if (isIntermediate)
                {   isIntermediate = calcIntermediatePos(); }
                if (!isIntermediate)
                {
                    if (!getNextPos())                      //  finish simu if nextPos = false
                    {   grbl.posMarker = (xyPoint)codeNext.actualPos;
                        grbl.posMarkerAngle = codeNext.alpha;
                        createMarkerPath(false, (xyPoint)codeNext.actualPos);
                        return -1;
                    }
                    if (remainingStep <= 0)
                        remainingStep += stepWidth;
                    remainingStep -= distance;   

                    if (remainingStep == 0)                 // just next full pos
                    {
                        grbl.posMarker = (xyPoint)codeNext.actualPos;
                        grbl.posMarkerAngle = codeNext.alpha;
                        isPenDownNow = codeNext.motionMode > 0;
                        createSimulationPath((xyPoint)codeNext.actualPos);
                        createMarkerPath(false, (xyPoint)codeNext.actualPos);
                        posXY = grbl.posMarker;
                        posA = grbl.posMarkerAngle;
                        return codeNext.lineNumber;    
                    }
                    else if (remainingStep > 0)             // move too short, get next gcode
                    {
                        isPenDownNow = codeNext.motionMode > 0;
                        createSimulationPath((xyPoint)codeNext.actualPos);
                        while (remainingStep > 0)
                        {
                            if (!getNextPos())              //  finish simu if nextPos = false //  calc distance & remaining steps
                            {   grbl.posMarker = (xyPoint)codeNext.actualPos;
                                grbl.posMarkerAngle = codeNext.alpha;
                                createMarkerPath(false, (xyPoint)codeNext.actualPos);
                                return -codeNext.lineNumber;
                            }
                            remainingStep -= distance;
                        }
                        if (remainingStep == 0)             // just next full pos
                        {   grbl.posMarker = (xyPoint)codeNext.actualPos;
                            grbl.posMarkerAngle = codeNext.alpha;
                            isPenDownNow = codeNext.motionMode > 0;
                            createSimulationPath((xyPoint)codeNext.actualPos);
                            createMarkerPath(false, (xyPoint)codeNext.actualPos);
                            posXY = grbl.posMarker;
                            posA = grbl.posMarkerAngle;
                            return codeNext.lineNumber;   
                        }
                    }
                    // remainingStep < 0 calc intermediate steps
                    posXY = (xyPoint)codeLast.actualPos;
                    posAngle = codeLast.alpha;
                    posZ = codeLast.actualPos.Z;
                    posA = codeLast.alpha;
					diff.XY = ((xyPoint)codeLast.actualPos).DistanceTo((xyPoint)codeNext.actualPos);
                    diff.Z  = Math.Abs(codeNext.actualPos.Z - codeLast.actualPos.Z);
                    diff.A   = Math.Abs(codeNext.alpha - codeLast.alpha);
                    isIntermediate = true;
                    calcIntermediatePos();
                    remainingStep = stepWidth;
                }
                grbl.posMarker = posXY;
                grbl.posMarkerAngle = posA ; // posAngle;
                isPenDownNow = codeNext.motionMode > 0;
                createSimulationPath(posXY);
                createMarkerPath(false, posXY);
                return codeNext.lineNumber;    
            }

/* Calculate intermediate position after fix time delay: simulationTimer = 50ms = 20Hz
   Step width depends on feedrate
*/ 
            private static bool calcIntermediatePos()
            {   updateFeedRate();
                remainingStep = stepWidth;
                double deltaA = codeNext.alpha - posA;
                isPenDownNow = codeNext.motionMode > 0;
                isG2G3 = codeNext.motionMode > 1;
                if (!isG2G3)
                {   double deltaS = posXY.DistanceTo((xyPoint)codeNext.actualPos);      // XY remaining max distance
                    if (tangentialAxisName != "Z")
                        deltaS = Math.Max(deltaS, Math.Abs(codeNext.actualPos.Z - posZ));   // Z  remaining max distance

                    if ((deltaS < remainingStep) && ((deltaA) < 0.1))       // return false if finish with intermediate
                    {   remainingStep -= deltaS;
                        createSimulationPath((xyPoint)codeNext.actualPos);
                        return false;
                    }
                    double deltaX = codeNext.actualPos.X - posXY.X;     // get remaining distance
                    double deltaY = codeNext.actualPos.Y - posXY.Y;
                    double deltaZ = codeNext.actualPos.Z - posZ;
                    //           double deltaA = codeNext.alpha - posA;
                    double dX = 0, dY = 0, dZ = 0, aStep = 10;
                    if (deltaS != 0)
                    {   dX = deltaX * remainingStep / deltaS;        // get step width relativ to max distance
                        dY = deltaY * remainingStep / deltaS;
                        dZ = deltaZ * remainingStep / deltaS;
                        aStep = diff.Max / remainingStep;               // amount of steps to reach end-value
                    }
                    posXY.X += dX;
                    posXY.Y += dY;
                    if (!checkWithin((xyPoint)codeLast.actualPos, (xyPoint)codeNext.actualPos, posXY))  // avoid going too far
                        posXY = (xyPoint)codeNext.actualPos;

                    posA += (codeNext.alpha - codeLast.alpha) / aStep;	// step width = 1/10	dA;
                    if ((codeNext.z != null) && !isTangentialZ)
                        posZ += dZ;
                    return true;
                }
                else
                {
                    double aStep2 = remainingStep / arcMove.radius;         // get delta angle
                    double turnAngle = arcMove.angleEnd - arcMove.angleStart;
                    if (codeNext.motionMode == 2)
                    {   if (turnAngle > 0)
                            turnAngle -= 2 * Math.PI;
                    }
                    else
                    {   if (turnAngle < 0)
                            turnAngle += 2 * Math.PI;
                    }

                    if (turnAngle == 0)
                        turnAngle = 2 * Math.PI;
                    double dA = Math.Abs((codeNext.alpha - codeLast.alpha) / (turnAngle /aStep2));	// get step width 

                    drawAngleOld = drawAngleNow;
                    if (arcMove.angleDiff > 0)
                    {
                        if (angleTmp < (arcMove.angleStart + arcMove.angleDiff))
                            angleTmp += aStep2;
                        if (posA < codeNext.alpha)
                            posA += dA;
                        if ((angleTmp >= (arcMove.angleStart + arcMove.angleDiff)) && (posA >= codeNext.alpha)) // return false if finish with intermediate
                        {
                            drawAngleNow = (float)((arcMove.angleStart + arcMove.angleDiff) * 180 / Math.PI);
                            createSimulationPath((xyPoint)codeNext.actualPos);
                            return false;
                        }
                    }
                    else
                    {
                        if (angleTmp > (arcMove.angleStart + arcMove.angleDiff))
                            angleTmp -= aStep2;
                        if (posA > codeNext.alpha)
                            posA -= dA;
                        if ((angleTmp <= (arcMove.angleStart + arcMove.angleDiff)) && (posA <= codeNext.alpha)) // return false if finish with intermediate
                        {
                            drawAngleNow = (float)((arcMove.angleStart + arcMove.angleDiff) * 180 / Math.PI);
                            createSimulationPath((xyPoint)codeNext.actualPos);
                            return false;
                        }
                    }
                    posXY.X = arcMove.center.X + arcMove.radius * Math.Cos(angleTmp);
                    posXY.Y = arcMove.center.Y + arcMove.radius * Math.Sin(angleTmp);
                    drawAngleNow = (float)(angleTmp * 180 / Math.PI);
                    return true;
                }
            }

            private static bool getNextPos()
            {   lineNr++;
                if (lineNr >= simuList.Count)
                    return false;
                codeLast = new gcodeByLine(codeNext);
                codeNext = new gcodeByLine(simuList[lineNr]);
                if (codeNext.codeLine.Contains("M30"))          // program end
                    return false;
                updateFeedRate();
                distance = getDistance();
                lastPosMarker = (xyPoint)codeLast.actualPos;
                if ((remainingStep - distance) > 0)
                {   isG2G3 = codeNext.motionMode > 1;
                    createSimulationPath((xyPoint)codeNext.actualPos);
                }
                return true;
            }

            public static xyPoint getLinePos(int lineNr)
            {
                if (lineNr >= simuList.Count)
                    return new xyPoint();
                return (xyPoint)simuList[lineNr].actualPos;
            }

            private static void updateFeedRate()
            {   feedRate = codeNext.feedRate;
                if (codeNext.motionMode == 0)
                    feedRate = feedRateRapid;
                stepWidth = feedRate * dt / 60000;   // feedrate in mm/min; dt in ms
            }

            private static double angleTmp=0;
            private static ArcProperties arcMove;
            private static double getDistance()
            {
                diff.Max = 0;
                diff.XY = ((xyPoint)codeLast.actualPos).DistanceTo((xyPoint)codeNext.actualPos);
                if (isTangentialZ)
                    diff.Z = 0;
                else
                    diff.Z = Math.Abs(codeNext.actualPos.Z - codeLast.actualPos.Z);
                diff.A = Math.Abs(codeNext.alpha - codeLast.alpha);

                if (diff.Max == 0)
                    diff.Max = diff.A;

                if (codeNext.motionMode < 2)
                {   diff.Max = Math.Max(diff.XY, diff.Z);
                    return diff.Max;   
                }
                arcMove = gcodeMath.getArcMoveProperties((xyPoint)codeLast.actualPos, (xyPoint)codeNext.actualPos, codeNext.i, codeNext.j, (codeNext.motionMode==2));
                angleTmp = arcMove.angleStart;
                drawAngleOld = drawAngleNow = (float)(angleTmp * 180 / Math.PI);
                diff.Arc = Math.Abs(arcMove.angleDiff * arcMove.radius);
                diff.Max = diff.Arc;
                return diff.Max;
            }

            private static xyPoint lastPosMarker = new xyPoint();
            public static void createSimulationPath(xyPoint moveto)
            {
                PointF start = new PointF((float)lastPosMarker.X,(float)lastPosMarker.Y);
                PointF end = new PointF((float)moveto.X, (float)moveto.Y);
                if ((isPenDownOld == false) && (isPenDownNow == true))
                {   pathSimulation.StartFigure(); }
                if (isPenDownNow)
                {
                    if (isG2G3)
                    {   float x1 = (float)(arcMove.center.X - arcMove.radius);
                        float x2 = (float)(arcMove.center.X + arcMove.radius);
                        float y1 = (float)(arcMove.center.Y - arcMove.radius);
                        float y2 = (float)(arcMove.center.Y + arcMove.radius);
                        float r2 = 2 * (float)arcMove.radius;

//                        Logger.Debug("x1:{0:0.00} y1:{1:0.00} r2:{2:0.00} start:{3:0.00} now:{4} sweep:{5:0.00}",x1, y1, r2,drawAngleOld, drawAngleNow, (drawAngleNow- drawAngleOld));
                        if (r2 > 0)
                            pathSimulation.AddArc(x1, y1, r2, r2, drawAngleOld, (drawAngleNow- drawAngleOld));
                        lastPosMarker = moveto;
                    }
                    else if (checkWithin((xyPoint)codeLast.actualPos, (xyPoint)codeNext.actualPos, moveto))
                    {   pathSimulation.AddLine(start, end);
                        lastPosMarker = moveto;
                    }
                }
                isPenDownOld = isPenDownNow;
            }
            private static bool checkWithin(xyPoint start, xyPoint end, xyPoint check)
            {
                if (check.X < Math.Min(start.X, end.X))
                    return false;
                if (check.X > Math.Max(start.X, end.X))
                    return false;
                if (check.Y < Math.Min(start.Y, end.Y))
                    return false;
                if (check.Y > Math.Max(start.Y, end.Y))
                    return false;
                return true;                
            }
        }
		
		public static class ProcessedPath
		{
		    private static xyzPoint lastPos = new xyzPoint();
            private static xyzPoint lastGCodePos = new xyzPoint();
            private static int lastLine = 0;
            private static int currentLine = 0;
            private static int maxLine = 0;
            private static byte lastMode = 0;
			
			private static int indexLastSucess = 0;
					
            public static void processedPathClear()
            {   Simulation.pathSimulation.Reset();
                lastLine = currentLine = maxLine = indexLastSucess = 0;
                lastPos = grbl.posWork;
            }

            public static void processedPathLine(int line)		// 
            {   maxLine = line;
//                Logger.Trace("  processedPathLine  {0} ", line);
            }

			private static int getNextXYIndex(int start)
			{	for (int i=start; i < simuList.Count; i++)
                {	if ((simuList[i].actualPos.X != simuList[start].actualPos.X) || (simuList[i].actualPos.Y != simuList[start].actualPos.Y))
						return i;
				}
                return start + 1;
			}

            public static void processedPathDraw(xyzPoint newPos)
            {
//                Logger.Trace("drawProcessedPath  {0:0.00}  {1:0.00}  {2:0.00}", newPos.X, newPos.Y, newPos.Z);
          //      int i = 0;
				int iStart, iEnd;
                bool onTrack = false;

                if ((lastPos.X == newPos.X) && (lastPos.Y == newPos.Y))
                    return;

                for (iStart = lastLine; iStart < maxLine; iStart++)
                {
                    iEnd = iStart + 1;
                    if (simuList[iEnd].motionMode <=1)
                        iEnd = getNextXYIndex(iStart);
                    onTrack = false;

                    if ((lastMode == 0) && (simuList[iStart].motionMode > 0))
                    {   Simulation.pathSimulation.StartFigure();    }

//                    Logger.Trace("  lastGCodePos x:{0:0.00} y:{1:0.00}  newPos x:{2:0.00} y:{3:0.00}   codePos iEnd:{4}  x:{5:0.00} y:{6:0.00}", lastGCodePos.X, lastGCodePos.Y, newPos.X, newPos.Y, iEnd, simuList[iEnd].actualPos.X, simuList[iEnd].actualPos.Y);
                    if (simuList[iEnd].motionMode > 1)
                    {
                        ArcProperties arcMove1, arcMove2;
                        arcMove1 = gcodeMath.getArcMoveProperties((xyPoint)simuList[iStart].actualPos, (xyPoint)simuList[iEnd].actualPos, simuList[iEnd].i, simuList[iEnd].j, (simuList[iEnd].motionMode == 2));
                        arcMove2 = gcodeMath.getArcMoveProperties((xyPoint)simuList[iStart].actualPos, (xyPoint)newPos,                   simuList[iEnd].i, simuList[iEnd].j, (simuList[iEnd].motionMode == 2));
                        onTrack = pointOnArc(arcMove1, arcMove2, toPointF(newPos)); 
//                        Logger.Trace(" arc ontrack iStart:{0}  iEnd:{1}  onTrack:{2}", iStart, iEnd, onTrack);
                    }
                    else if (simuList[iEnd].motionMode == 1)
                    {   onTrack = pointOnLine(toPointF(simuList[iStart].actualPos), toPointF(simuList[iEnd].actualPos), toPointF(newPos));
                  //      Logger.Trace(" line ontrack i{0} x:{1:0.00} y:{2:0.00} {3}", i, simuList[i].actualPos.X, simuList[i].actualPos.Y, onTrack);
                    }

                    if (onTrack)
                    {   // newPos is on line towards next GCode command
 //                       Logger.Trace(" drawProcessedPath  true iStart:{0} iEnd:{1}  {2:0.00}  {3:0.00}  {4:0.00}",iStart, iEnd, newPos.X, newPos.Y, newPos.Z);
						indexLastSucess = iStart;
						for (int k = lastLine+1; k < iEnd; k++)
                        {
                            if ((lastGCodePos.X == simuList[k].actualPos.X) && (lastGCodePos.Y == simuList[k].actualPos.Y) && (simuList[k].motionMode < 2))
                            {   lastMode = simuList[k].motionMode;
//                                Logger.Trace("  same pos {0}",k);
                                continue; }

//                            Logger.Trace(" drawProcessedPath  k:{0} x:{1:0.00}  y:{2:0.00} mode:{3}", k, simuList[k].actualPos.X, simuList[k].actualPos.Y, simuList[k].motionMode);
                            if (simuList[k].motionMode == 0)
                            {
                                lastGCodePos = new xyzPoint(simuList[k].actualPos.X, simuList[k].actualPos.Y, simuList[k].actualPos.Z);
                                Simulation.pathSimulation.StartFigure();
//                                Logger.Trace("  startFigure");
                            }
                            else if (simuList[k].motionMode == 1)
                            {
                                Simulation.pathSimulation.AddLine(toPointF(lastGCodePos), toPointF(simuList[k].actualPos));
//                                Logger.Trace(" AddLine x:{0:0.00}  y:{1:0.00} to x:{2:0.00}  y:{3:0.00}", lastGCodePos.X, lastGCodePos.Y, simuList[k].actualPos.X, simuList[k].actualPos.Y);
                            }
                            else if (simuList[k].motionMode >= 2)
                            {
                                ArcProperties arcMove;
                                arcMove = gcodeMath.getArcMoveProperties((xyPoint)lastGCodePos, (xyPoint)simuList[k].actualPos, simuList[k].i, simuList[k].j, (simuList[k].motionMode == 2));

                                float x1 = (float)(arcMove.center.X - arcMove.radius);
                                float x2 = (float)(arcMove.center.X + arcMove.radius);
                                float y1 = (float)(arcMove.center.Y - arcMove.radius);
                                float y2 = (float)(arcMove.center.Y + arcMove.radius);
                                float r2 = 2 * (float)arcMove.radius;
                                float aStart = (float)(arcMove.angleStart * 180 / Math.PI);
                                float aDiff = (float)(arcMove.angleDiff * 180 / Math.PI);
                                if (arcMove.radius > 0)
                                {
                                    Simulation.pathSimulation.AddArc(x1, y1, r2, r2, aStart, aDiff);
//                                    Logger.Trace(" AddArc x:{0:0.00}  y:{1:0.00} r:{2:0.00}  ", x1, y1, r2);
                                }
                            }
                            lastGCodePos = new xyzPoint(simuList[k].actualPos.X, simuList[k].actualPos.Y, simuList[k].actualPos.Z);
                        }


                        if (simuList[iEnd].motionMode == 1)
                        {   Simulation.pathSimulation.AddLine(toPointF(lastGCodePos), toPointF(newPos));
//                            Logger.Trace("   AddLine newPos  i:{0}  x:{1:0.00}  y:{2:0.00} to x:{3:0.00}  y:{4:0.00}", iEnd, lastGCodePos.X, lastGCodePos.Y, newPos.X, newPos.Y);
                        }
                        else if (simuList[iEnd].motionMode > 1)
                        {   Simulation.pathSimulation.AddLine(toPointF(lastPos), toPointF(newPos));
//                            Logger.Trace("   AddArc newPos  i:{0}  x:{1:0.00}  y:{2:0.00} to x:{3:0.00}  y:{4:0.00}", iEnd, lastPos.X, lastPos.Y, newPos.X, newPos.Y);
                        }
                        //      if (i > 1)
                        lastLine = iStart;		//200720
                        lastMode = simuList[iStart].motionMode;
                        lastPos = newPos;
                        indexLastSucess = iStart;
                        break;
                    }
                }				
				// no match found, try new start-pos
				if (!onTrack)	
				{	lastGCodePos = new xyzPoint(simuList[indexLastSucess].actualPos.X, simuList[indexLastSucess].actualPos.Y, simuList[indexLastSucess].actualPos.Z);}				
            }
            private static PointF toPointF(xyzPoint tmp)
            { return new PointF((float)tmp.X, (float)tmp.Y); }
            private static PointF toPointF(xyzabcuvwPoint tmp)
            { return new PointF((float)tmp.X, (float)tmp.Y); }

            private static bool pointOnArc(ArcProperties arcMove1, ArcProperties arcMove2, PointF xp)
            {
                double dx = arcMove1.center.X - xp.X;
                double dy = arcMove1.center.Y - xp.Y;
                double r = Math.Sqrt(dx * dx + dy * dy);
//                Logger.Trace("   pointOnArc  origR:{0:0.00}  xpR:{1:0.00}  aStart:{2:0.00} aEnd:{3:0.00}  xpEnd:{4:0.00}", arcMove1.radius, r, arcMove1.angleStart, arcMove1.angleEnd, arcMove2.angleEnd);

                if (!isEqual(r, arcMove1.radius))
                    return false;

                if (arcMove1.angleEnd == arcMove1.angleStart)
                    return true;

                if (arcMove2.angleEnd < arcMove1.angleStart)
                    return false;
                if (arcMove2.angleEnd > arcMove1.angleEnd)
                    return false;

                return true;
            }

            private static bool pointOnLine(PointF p1,PointF p2, PointF xp)
            {
                if ((xp.X < Math.Min(p1.X, p2.X)) || (xp.X > Math.Max(p1.X, p2.X)))
                {
          //          Logger.Trace("  pointOnLine false ((xp.X < p1.X) || (xp.X > p2.X))  p1:{0:0.00}  xp:{1:0.00}  p2:{2:0.00}", p1.X, xp.X, p2.X);
                    if (isEqual(xp.X, Math.Min(p1.X, p2.X))) return true;
                    if (isEqual(xp.X, Math.Max(p1.X, p2.X))) return true;
                    return false;
                }
                if ((xp.Y < Math.Min(p1.Y, p2.Y)) || (xp.Y > Math.Max(p1.Y, p2.Y)))
                {
       //             Logger.Trace("  pointOnLine false ((xp.Y < p1.Y) || (xp.Y > p2.Y))  p1:{0:0.00}  xp:{1:0.00}  p2:{2:0.00}", p1.Y, xp.Y, p2.Y);
                    if (isEqual(xp.Y, Math.Min(p1.Y, p2.Y))) return true;
                    if (isEqual(xp.Y, Math.Max(p1.Y, p2.Y))) return true;
                    return false;
                }
                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                if (isEqual(dx, 0))
                {
      //              Logger.Trace("  pointOnLine  (isEqual(dx, 0))");
                    return (isEqual(p1.X, xp.X));
                }
                if (isEqual(dy, 0))
                {
       //             Logger.Trace("  pointOnLine  (isEqual(dy, 0))");
                    return (isEqual(p1.Y, xp.Y));
                }

                double m = dy / dx;
                double b = p1.Y - (m * p1.X);
                double y = m * xp.X + b;

/*                Logger.Trace("  pointOnLine  p1 x:{0:0.00}  y:{1:0.00}", p1.X, p1.Y);
                Logger.Trace("  pointOnLine  p2 x:{0:0.00}  y:{1:0.00}", p2.X, p2.Y);
                Logger.Trace("  pointOnLine  xp x:{0:0.00}  y:{1:0.00}", xp.X, xp.Y);
                Logger.Trace("  pointOnLine  grbl x:{0:0.000}  y:{1:0.000}", grbl.posWork.X, grbl.posWork.Y);
                Logger.Trace("  pointOnLine  isEqual(y, xp.Y) y:{0:0.00}  xp.Y:{1:0.00}",y,xp.Y);
*/                return (isEqual(y, xp.Y));
            }
            private static bool isEqual(double a, double b, double max= 0.1)
            {
                if (Math.Abs(a - b) < max)
                    return true;
                return false;
            }

        }
    }

    /// <summary>
    /// Calculate equidistant line = cutter radius compensation
    /// </summary>
    public static class crc   // cutter radius compensation
    {   // http://www.hinterseher.de/Diplomarbeit/GeometrischeFunktionen.html

        // Trace, Debug, Info, Warn, Error, Fatal
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool logenable = ((uint)Properties.Settings.Default.importLoggerSettings & 32) > 0;

        // get two lines and calc offsetted points
        public static int getPointOffsets(ref xyPoint[] offset, xyArcPoint P0, xyArcPoint P1, xyArcPoint P2, double distance, bool isEnd)
        {
            xyPoint[] S1off = new xyPoint[2];
            xyPoint[] S2off = new xyPoint[2];

            double a0 = 0, a1 = 0, a2 = 0, a3 = 0, adelta;
            double newRadius1 = distance;
            double newRadius2 = distance;

            if (P1.mode <= 1)   // is a line
            {
                a1 = getAlphaLine(P0, P1);
                a0 = a1;
                calcOffsetLine(ref S1off, P0, P1, a1 + Math.PI / 2, distance); // offset by 90°
            }
            else
            {
                a0 = getAlphaCenterToPoint(P1, P0);    // from center to start
                a1 = getAlphaCenterToPoint(P1, P1);    // from center to end

                double usea0 = a0, usea1 = a1;
                a0 -= Math.PI / 2; a1 -= Math.PI / 2;   // tangente

                if (P1.mode == 3)
                {
                    usea0 += Math.PI; usea1 += Math.PI; // add 180°
                    a0 += Math.PI; a1 += Math.PI;        // tangente reverse
                }

                S1off[0] = calcOffsetPoint(P0, usea0, distance); // extend radius
                S1off[1] = calcOffsetPoint(P1, usea1, distance); // extend radius
                newRadius1 = Math.Sqrt((P1.CX - S1off[0].X) * (P1.CX - S1off[0].X) + (P1.CY - S1off[0].Y) * (P1.CY - S1off[0].Y));
            }
            offset[0] = S1off[0];
            offset[1] = S1off[1];

   //         Logger.Trace(" getPointOffsets P0-P1: P1mode: {0} S1offX {1:0.000} S1offX {2:0.000} S1offX {3:0.000} S1offX {4:0.000}", P1.mode, S1off[0].X, S1off[0].Y, S1off[1].X, S1off[1].Y);

            if (P2.mode <= 1)   // is a line
            {
                a2 = getAlphaLine(P1, P2);
                a3 = a2;
                calcOffsetLine(ref S2off, P1, P2, a2 + Math.PI / 2, distance); // offset by 90°
            }
            else
            {
                a2 = getAlphaCenterToPoint(P2, P1);   // from center to start
                a3 = getAlphaCenterToPoint(P2, P2);   // from center to end
                double usea2 = a2, usea3 = a3;
                a2 -= Math.PI / 2; a3 -= Math.PI / 2;   // tangente

                if (P2.mode == 3)
                {
                    usea2 += Math.PI; usea3 += Math.PI;     // add 180°
                    a2 += Math.PI; a3 += Math.PI;        // tangente reverse
                }

                S2off[0] = calcOffsetPoint(P1, usea2, distance); // extend radius
                S2off[1] = calcOffsetPoint(P2, usea3, distance); // extend radius
                newRadius2 = Math.Sqrt((P2.CX - S2off[0].X) * (P2.CX - S2off[0].X) + (P2.CY - S2off[0].Y) * (P2.CY - S2off[0].Y));
            }
            offset[2] = S2off[0];
            offset[3] = S2off[1];

            if (logenable) Logger.Trace(" getPointOffsets P1-P2: P2mode: {0} S1offX {1:0.000} S1offX {2:0.000} S1offX {3:0.000} S1offX {4:0.000}", P2.mode, S2off[0].X, S2off[0].Y, S2off[1].X, S2off[1].Y);

            if ((P1.mode == P2.mode) && (P1.X == P2.X) && (P1.Y == P2.Y))
            { a2 = a0; a3 = a1; }

            // compare angle of both lines P0-P1 and P1-P2
            adelta = a2 - a1;
            double dist = offset[1].DistanceTo(offset[2]);

            if (logenable) Logger.Trace(" getPointOffsets Angles: a1 {0:0.000} a2 {1:0.000} delta {2:0.000}", (a1*180/Math.PI),(a2 * 180 / Math.PI), (adelta * 180 / Math.PI));

            if (adelta >= (Math.PI))
                adelta -= 2 * Math.PI;
            if (adelta <= -(Math.PI))
                adelta += 2 * Math.PI;

            if (logenable) Logger.Trace(" getPointOffsets adelta corrected {0:0.000}", (adelta * 180 / Math.PI));
            if (logenable) Logger.Trace(" getPointOffsets offset [0]x{0:0.000} [0]y{1:0.000} [1]x{2:0.000} [1]y{3:0.000} [2]x{4:0.000} [2]y{5:0.000}", offset[0].X, offset[0].Y, offset[1].X, offset[1].Y, offset[2].X, offset[2].Y);

            if (isEnd || (Math.Abs(adelta) <= 0.2) || (dist < 0.2))
                return 0;           // S1-angle == S2-angle, no correction needed

            if (Math.Abs(Math.Abs(adelta) -Math.PI) <= 0.2)
                return 1;           // 180°

            if ((adelta > 0) && (distance < 0))
                return 1;           // connect lines with additional arc
            if ((adelta < 0) && (distance > 0))
                return 1;           // connect lines with additional arc

            if (logenable) Logger.Trace(" getPointOffsets Find intersection {0} {1}", P1.mode, P2.mode);

            bool result = false;
            // find common intersection
            if ((P1.mode <= 1) && (P2.mode <= 1))   // line to line
            {   // https://www.java-forum.org/thema/algorithmus-fuer-pruefung-auf-ueberschneidende-linien.117102/
                double d = (S1off[1].X - S1off[0].X) * (S2off[0].Y - S2off[1].Y) - (S2off[0].X - S2off[1].X) * (S1off[1].Y - S1off[0].Y);
                if (d == 0)
                { offset[2] = offset[1] = offset[3]; }
                else
                {
                    double m = ((S2off[0].X - S1off[0].X) * (S2off[0].Y - S2off[1].Y) - (S2off[0].X - S2off[1].X) * (S2off[0].Y - S1off[0].Y)) / d;
                    double n = ((S1off[1].X - S1off[0].X) * (S2off[0].X - S1off[0].X) - (S2off[0].Y - S1off[0].Y) * (S1off[1].Y - S1off[0].Y)) / d;
                    offset[1].X = S1off[0].X + m * (S1off[1].X - S1off[0].X);
                    offset[1].Y = S1off[0].Y + m * (S1off[1].Y - S1off[0].Y);
                    offset[2] = offset[1];
                }
            }
            else if ((P1.mode <= 1) && (P2.mode >= 2))    // 1st line then arc
            {
                result = calcIntersectionLineArc(ref offset, S1off, P2, newRadius2);
            }
            else if ((P1.mode >= 2) && (P2.mode <= 1))      // 1st arc then line
            {
                xyPoint tmp = S2off[0];
                S2off[0] = S2off[1];S2off[1] = tmp; // switch points, p[1] should be connection to arc
                result = calcIntersectionLineArc(ref offset, S2off, P1, newRadius1);
            }
            else
            {   // 1st arc 2nd arc, transfer one arc to line to use available function calcIntersectionLineArc
                // http://www2.math.uni-wuppertal.de/~volkert/Das%20Apollonische%20Beruehrproblem,%202007.pdf
                double dy = P2.CY - P1.CY;
                double dx = P2.CX - P1.CX;
                if (dy == 0)        // center points of arcs on same y -> chordale = vertical line
                {
                    double a = (newRadius2 * newRadius2 - newRadius1 * newRadius1 - dx * dx) / (-2 * dx);
                    double px = P1.CX + a;  // vertical line
                    S1off[0].X = S1off[1].X = px;
                    S1off[0].Y = -P1.Y;
                    S1off[1].Y = P1.Y;
                    result = calcIntersectionLineArc(ref offset, S1off, P2, newRadius2);
                }
                else if (dx == 0)   // center points of arcs on same x -> chordale = horizontal line
                {
                    double a = (newRadius2 * newRadius2 - newRadius1 * newRadius1 - dy * dy) / (-2 * dy);
                    double py = P1.CY + a;  // horizontal line
                    S1off[0].Y = S1off[1].Y = py;
                    S1off[0].X = -P1.X;
                    S1off[1].X = P1.X;
                    result = calcIntersectionLineArc(ref offset, S1off, P2, newRadius2);
                }
                else
                {
                    double c = Math.Sqrt(dx * dx + dy * dy);
                    double a = (newRadius2 * newRadius2 - newRadius1 * newRadius1 - c * c) / (-2 * c);
                    double m = dy / dx;
                    double angle = getAlphaCenterToCenter(P1, P2);
                    xyPoint aP = new xyPoint();
                    aP = calcOffsetPoint(new xyPoint(P1.CX, P1.CY), angle, a);
                    angle += Math.PI / 2;

                    S1off[0] = calcOffsetPoint(aP, angle, newRadius1);  // create line from point
                    S1off[1] = calcOffsetPoint(aP, angle, -newRadius1); // create line from point
                    double d0 = S1off[0].DistanceTo((xyPoint)P1);
                    double d1 = S1off[1].DistanceTo((xyPoint)P1);

                    if (d1 > d0)    // index 1 should be closer to final pos
                    {
                        S1off[1] = calcOffsetPoint(aP, angle, newRadius1);  // create line from point
                        S1off[0] = calcOffsetPoint(aP, angle, -newRadius1); // create line from point
                    }
                    result = calcIntersectionLineArc(ref offset, S1off, P2, newRadius2);
                }

                if ((double.IsNaN(offset[1].X)) || double.IsNaN(offset[1].Y))
                {
                    offset[1].X = 0; offset[1].Y = 0;
                    offset[2] = offset[1];
                }
            }
            if (result == true) // intersection successful
                return -1;
            else
                return -2;
        }

        private static bool calcIntersectionLineArc(ref xyPoint[] resultOffset, xyPoint[] linePoint, xyArcPoint arc, double radius) // return success
        {   // circular equation: r^2 = (x-xm)^2 + (y - ym)^2 = r^2    =>    y = ym ± √(r2 - (x-xm)2) 
            // linear equation: y = m*x + n 
            double x = 0, x1 = 0, x2 = 0, y = 0, y1 = 0, y2 = 0;
            double dx = (linePoint[1].X - linePoint[0].X);
            double dy = (linePoint[1].Y - linePoint[0].Y);

            if (logenable) Logger.Trace("   calcIntersectionLineArc 0x {0:0.00} 0y {1:0.00} 1x {2:0.00} 1y {3:0.00} Arcx {4:0.00} Arcy {5:0.00} ArcCx {6:0.00} ArcCy {7:0.00}", linePoint[0].X, linePoint[0].Y, linePoint[1].X, linePoint[1].Y,arc.X,arc.Y, arc.CX, arc.CY);
            if (logenable) Logger.Trace("   dx {0:0.00} dy {1:0.00} ", dx,dy);

            if (dx == 0)        // vertical line, x is known
            {
                double a2minusb2 = getA2minusB2(radius, (linePoint[0].X - arc.CX));
                if (a2minusb2 >= 0)
                {
                    double tmpRoot = Math.Sqrt(a2minusb2);//  getRoot(radius, (linePoint[0].X - arc.CX));
                    y1 = arc.CY + tmpRoot;   // y = ym ± √(r2 - (x-xm)2) 
                    y2 = arc.CY - tmpRoot;
                    y = y1;
                    if (Math.Abs(linePoint[1].Y - y2) < Math.Abs(linePoint[1].Y - y1))  // find closer point
                        y = y2;
                    resultOffset[1].X = linePoint[0].X;
                    resultOffset[1].Y = y;
                    resultOffset[2] = resultOffset[1];

                    if (logenable) Logger.Trace("   intersection at x{0:0.000} y{1:0.000}", resultOffset[1].X, resultOffset[1].Y);

                    return true;
                }
                else
                {
                    resultOffset[1].X = linePoint[0].X;
                    resultOffset[1].Y = resultOffset[3].Y;// arc.Y-radius;
                    resultOffset[2] = resultOffset[1];

                    if (logenable) Logger.Trace("   no intersection! ");

                    return false;
                }
            }

            else if (dy == 0)   // horizontal line, y is known
            {
                double a2minusb2 = getA2minusB2(radius, (linePoint[0].Y - arc.CY));
                if (a2minusb2 >= 0)
                {
                    double tmpRoot = Math.Sqrt(a2minusb2);
                    x1 = arc.CX + tmpRoot;// getRoot(radius, (linePoint[0].Y - arc.CY));   // y = ym ± √(r2 - (x-xm)2) 
                    x2 = arc.CX - tmpRoot;// getRoot(radius, (linePoint[0].Y - arc.CY));
                    x = x1;
                    if (Math.Abs(linePoint[1].X - x2) < Math.Abs(linePoint[1].X - x1))
                        x = x2;
                    resultOffset[1].X = x;
                    resultOffset[1].Y = linePoint[0].Y;
                    resultOffset[2] = resultOffset[1];

                    if (logenable) Logger.Trace("   intersection at x{0:0.000} y{1:0.000}", resultOffset[1].X, resultOffset[1].Y);

                    return true;
                }
                else
                {
                    resultOffset[1].X = resultOffset[2].X;// arc.X - radius;
                    resultOffset[1].Y = linePoint[0].Y; 
                    resultOffset[2] = resultOffset[1];

                    if (logenable) Logger.Trace("   no intersection! ");

                    return false;
                }
            }
            else
            {   // intersection line-arc
                // circular equation: r^2 = (x-xm)^2 + (y - ym)^2 = r^2    =>    y = ym ± √(r2 - (x-xm)2) 
                // linear equation: y = m*x + n     =>   n = y - m*x
                resultOffset[2] = resultOffset[1] = resultOffset[0];
                double m = dy / dx;                                 // y=m*x+n
                double n = linePoint[1].Y - m * linePoint[1].X;     // n=y-m*x
                double a = 1 + m * m;                               // r²=(x-cx)² + (y-cy)²
                double b = 2 * (m * n - arc.CX - arc.CY * m);       // 0=x²-2*x*cx+cx² + y²-2*y*cy+cy²
                double c = arc.CX * arc.CX + arc.CY * arc.CY + n * n - radius * radius - 2 * arc.CY * n;
                double a2minusb2 = getA2minusB2((b * b),(4 * a * c));
                if (a2minusb2 >= 0)
                {

                    double root = Math.Sqrt((b * b) - (4 * a * c));

                    x1 = (-b + root) / (2 * a);           // ax²+bx+c=0
                    x2 = (-b - root) / (2 * a);           // x=(-b±√b²-4ac)/(2a)
                    x = x1;

                    if (Math.Abs(linePoint[1].X - x2) < Math.Abs(linePoint[1].X - x1))
                        x = x2;

                    if (logenable) Logger.Trace("   x1 {0:0.00} x2 {1:0.00} lp1x {2:0.00} x {3:0.00}", x1, x2, linePoint[1].X, x);

                    y = m * x + n;
                    resultOffset[1].X = x; resultOffset[1].Y = y;
                    resultOffset[2] = resultOffset[1];
                    return true;
                }
                else
                { return false; }
            }
        }

        private static void calcOffsetLine(ref xyPoint[] offset, xyArcPoint P0, xyArcPoint P1, double angle, double radius)
        {
            offset[0].X = P0.X + Math.Cos(angle) * radius;   // 1st point of 1st line
            offset[0].Y = P0.Y + Math.Sin(angle) * radius;
            offset[1].X = P1.X + Math.Cos(angle) * radius;   // 2nd point of 1st line
            offset[1].Y = P1.Y + Math.Sin(angle) * radius;
        }

        private static xyPoint calcOffsetPoint(xyArcPoint P, double angle, double radius)
        { return calcOffsetPoint(new xyPoint(P.X, P.Y), angle, radius); }
        private static xyPoint calcOffsetPoint(xyPoint P, double angle, double radius)
        {
            xyPoint tmp = new xyPoint();
            tmp.X = P.X + Math.Cos(angle) * radius;
            tmp.Y = P.Y + Math.Sin(angle) * radius;
            return tmp;
        }
        private static double getA2minusB2(double a, double b)
        { return (a * a - b * b); }
        private static double getRoot(double radius, double b)
        { return Math.Sqrt(radius * radius - b * b); }

        private static double getAlphaLine(xyArcPoint P1, xyArcPoint P2)
        { return getAlpha(P1.X, P1.Y, P2.X, P2.Y); }
        private static double getAlphaCenterToPoint(xyArcPoint P1, xyArcPoint P2)
        { return getAlpha(P1.CX, P1.CY, P2.X, P2.Y); }
        private static double getAlphaCenterToCenter(xyArcPoint P1, xyArcPoint P2)
        { return getAlpha(P1.CX, P1.CY, P2.CX, P2.CY); }

        private static double getAlpha(double P1x, double P1y, double P2x, double P2y)
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
            }
            else if (dy == 0)
            {
                if (dx > 0)
                    a = 0;
                else
                    a = Math.PI;
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
    }

}
