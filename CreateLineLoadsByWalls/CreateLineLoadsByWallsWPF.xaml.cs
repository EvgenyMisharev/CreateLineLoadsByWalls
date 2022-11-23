using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreateLineLoadsByWalls
{
    public partial class CreateLineLoadsByWallsWPF : Window
    {
        public Document SelectedLinkDoc;
        public RevitLinkInstance SelectedRevitLinkInstance;
        public List<WallType> SelectedWallTypes;
        public double Fx;
        public double Fy;
        public double Fz;

        public double Mx;
        public double My;
        public double Mz;

        public CreateLineLoadsByWallsWPF(List<RevitLinkInstance> revitLinkInstanceList)
        {
            InitializeComponent();

            listBox_RevitLinkInstance.ItemsSource = revitLinkInstanceList;
            listBox_RevitLinkInstance.DisplayMemberPath = "Name";
        }

        private void listBox_RevitLinkInstance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedRevitLinkInstance = listBox_RevitLinkInstance.SelectedItem as RevitLinkInstance;
            SelectedLinkDoc = SelectedRevitLinkInstance.GetLinkDocument();

            List<WallType> wallTypesList = new FilteredElementCollector(SelectedLinkDoc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Where(wt => new FilteredElementCollector(SelectedLinkDoc)
                        .OfCategory(BuiltInCategory.OST_Walls)
                        .OfClass(typeof(Wall))
                        .WhereElementIsNotElementType()
                        .Cast<Wall>()
                        .Count(w => w.WallType.Id == wt.Id) != 0)
                .OrderBy(wt => wt.Name, new AlphanumComparatorFastString())
                .ToList();

            listBox_WallTypes.ItemsSource = wallTypesList;
            listBox_WallTypes.DisplayMemberPath = "Name";
        }

        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            this.DialogResult = true;
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                SaveSettings();

                this.DialogResult = true;
                this.Close();
            }

            else if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void SaveSettings()
        {
            SelectedWallTypes = listBox_WallTypes.SelectedItems.Cast<WallType>().ToList();
            double.TryParse(textBox_Fx.Text, out Fx);
            double.TryParse(textBox_Fy.Text, out Fy);
            double.TryParse(textBox_Fz.Text, out Fz);
            double.TryParse(textBox_Mx.Text, out Mx);
            double.TryParse(textBox_My.Text, out My);
            double.TryParse(textBox_Mz.Text, out Mz);
        }
    }
}
