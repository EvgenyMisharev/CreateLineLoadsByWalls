using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateLineLoadsByWalls
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class CreateLineLoadsByWallsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            List<RevitLinkInstance> revitLinkInstanceList = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();
            if (revitLinkInstanceList.Count == 0)
            {
                TaskDialog.Show("Ravit", "В проекте отсутствуют связанные файлы!");
                return Result.Cancelled;
            }

            List<LineLoadType> lineLoadTypeList = new FilteredElementCollector(doc)
                .OfClass(typeof(LineLoadType))
                .Cast<LineLoadType>()
                .ToList();

            CreateLineLoadsByWallsWPF createLineLoadsByWallsWPF = new CreateLineLoadsByWallsWPF(revitLinkInstanceList);
            createLineLoadsByWallsWPF.ShowDialog();
            if (createLineLoadsByWallsWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }

            Document selectedLinkDoc = createLineLoadsByWallsWPF.SelectedLinkDoc;
            RevitLinkInstance selectedRevitLinkInstance = createLineLoadsByWallsWPF.SelectedRevitLinkInstance;
            Transform transform = selectedRevitLinkInstance.GetTotalTransform();

            List<WallType> selectedWallTypes = createLineLoadsByWallsWPF.SelectedWallTypes;
            double Fx = createLineLoadsByWallsWPF.Fx * 1000;
            double Fy = createLineLoadsByWallsWPF.Fy * 1000;
            double Fz = createLineLoadsByWallsWPF.Fz * 1000;

            double Mx = createLineLoadsByWallsWPF.Mx * 1000;
            double My = createLineLoadsByWallsWPF.My * 1000;
            double Mz = createLineLoadsByWallsWPF.Mz * 1000;

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Линейная нагрузка по стене");
                foreach(WallType wallType in selectedWallTypes)
                {
                    List<Wall> wallList = new FilteredElementCollector(selectedLinkDoc)
                        .OfCategory(BuiltInCategory.OST_Walls)
                        .OfClass(typeof(Wall))
                        .WhereElementIsNotElementType()
                        .Cast<Wall>()
                        .Where(w => w.WallType.Id == wallType.Id)
                        .ToList();

                    foreach(Wall wall in wallList)
                    {
                        string wallHeightStr = "";
                        if (wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId().IntegerValue != -1)
                        {
                            double wallBaseOffset = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble();
                            if (wallBaseOffset > 0)
                            {
                                wallBaseOffset = - wallBaseOffset;
                            }
                            else
                            {
                                wallBaseOffset = Math.Abs(wallBaseOffset);
                            }
                            double wallBaseLevelElevation = (selectedLinkDoc.GetElement(wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId()) as Level).Elevation;

                            double wallTopOffset = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).AsDouble();
                            double wallTopLevelElevation = (selectedLinkDoc.GetElement(wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId()) as Level).Elevation;

                            wallHeightStr = ((int)(Math.Round((wallTopLevelElevation - wallBaseLevelElevation) + wallBaseOffset + wallTopOffset, 6) * 304.8)).ToString();
                        }
                        else
                        {
                            double wallBaseOffset = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble();
                            if(wallBaseOffset > 0)
                            {
                                wallBaseOffset = - wallBaseOffset;
                            }
                            else
                            {
                                wallBaseOffset = Math.Abs(wallBaseOffset);
                            }
                            double wallUserHeight = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
                            wallHeightStr = ((int)(Math.Round(wallBaseOffset + wallUserHeight, 6) * 304.8)).ToString();
                        }
                        if ((wall.Location as LocationCurve).Curve.IsCyclic == true) continue;

                        XYZ lineP1 = ((wall.Location as LocationCurve).Curve as Line).GetEndPoint(0);
                        XYZ lineP2 = ((wall.Location as LocationCurve).Curve as Line).GetEndPoint(1);
                        XYZ resultLineP1 = transform.OfPoint(new XYZ(lineP1.X, lineP1.Y, lineP1.Z + wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble()));
                        XYZ resultLineP2 = transform.OfPoint(new XYZ(lineP2.X, lineP2.Y, lineP2.Z + wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble()));

                        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                        SketchPlane skplane = SketchPlane.Create(doc, plane);
                        LineLoad lineLoad = LineLoad.Create(doc, resultLineP1, resultLineP2, XYZ.BasisZ, XYZ.BasisY, lineLoadTypeList.First(), skplane);

                        lineLoad.get_Parameter(BuiltInParameter.LOAD_USE_LOCAL_COORDINATE_SYSTEM).Set(0);
                        lineLoad.get_Parameter(BuiltInParameter.LOAD_IS_UNIFORM).Set(1);
                        lineLoad.get_Parameter(BuiltInParameter.LOAD_IS_PROJECTED).Set(0);
                        lineLoad.get_Parameter(BuiltInParameter.LOAD_IS_REACTION).Set(0);

                        lineLoad.get_Parameter(BuiltInParameter.LOAD_LINEAR_FORCE_FX1).Set(Fx);
                        lineLoad.get_Parameter(BuiltInParameter.LOAD_LINEAR_FORCE_FY1).Set(Fy);
                        lineLoad.get_Parameter(BuiltInParameter.LOAD_LINEAR_FORCE_FZ1).Set(Fz);

                        lineLoad.get_Parameter(BuiltInParameter.LOAD_MOMENT_MX1).Set(Mx);
                        lineLoad.get_Parameter(BuiltInParameter.LOAD_MOMENT_MY1).Set(My);
                        lineLoad.get_Parameter(BuiltInParameter.LOAD_MOMENT_MZ1).Set(Mz);

                        lineLoad.get_Parameter(BuiltInParameter.LOAD_DESCRIPTION).Set(wallType.Name);
                        lineLoad.get_Parameter(BuiltInParameter.LOAD_COMMENTS).Set($"{wallHeightStr} мм");
                    }

                }
                t.Commit();
            }
            return Result.Succeeded;
        }
    }
}
