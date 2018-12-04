using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MapInfo.Mapping;
using MapInfo.Engine;
using MapInfo.Data;
using MapInfo.Geometry;
using MapInfo.Styles;
using System.IO;

namespace Eternal.MapApp
{
    public partial class MainMap : Form
    {
        FeatureLayer eagleEye;
        Feature fRec;

        public MainMap()
        {
            InitializeComponent();
            mapControl1.Map.ViewChangedEvent += new MapInfo.Mapping.ViewChangedEventHandler(Map_ViewChangedEvent);
            Map_ViewChangedEvent(this, null);
            this.mapControl2.MouseWheelSupport = new MouseWheelSupport(MouseWheelBehavior.None, 10, 5);
        }

        void Map_ViewChangedEvent(object sender, MapInfo.Mapping.ViewChangedEventArgs e)
        {
            // Display the zoom level
            Double dblZoom = System.Convert.ToDouble(String.Format("{0:E2}", mapControl1.Map.Zoom.Value));
            if (statusStrip1.Items.Count > 0)
            {
                statusStrip1.Items[0].Text = "缩放: " + dblZoom.ToString() + " " + MapInfo.Geometry.CoordSys.DistanceUnitAbbreviation(mapControl1.Map.Zoom.Unit);
            }

            if (Session.Current.Catalog.GetTable("EagleEyeTemp") == null)
                loadEagleLayer();
            Table tblTemp = Session.Current.Catalog.GetTable("EagleEyeTemp");
            try
            {
                (tblTemp as ITableFeatureCollection).Clear();
            }
            catch (Exception)
            { }

            try
            {
                // 主地图发生改变是对鹰眼地图进行相应的画框操作
                if (mapControl2.Map.Layers["MyEagleEye"] != null)
                    mapControl2.Map.Layers.Remove(eagleEye);
                DRect rect = mapControl1.Map.Bounds;
                FeatureGeometry fg = new MapInfo.Geometry.Rectangle(mapControl1.Map.GetDisplayCoordSys(), rect);
                SimpleLineStyle vLine = new SimpleLineStyle(new LineWidth(2, LineWidthUnit.Point), 2, Color.Red);
                SimpleInterior vInter = new SimpleInterior(9, Color.Yellow, Color.Yellow, true);
                CompositeStyle cStyle = new CompositeStyle(new AreaStyle(vLine, vInter), null, null, null);
                fRec = new Feature(fg, cStyle);
                tblTemp.InsertFeature(fRec);
                eagleEye = new FeatureLayer(tblTemp, "EagleEye ", "MyEagleEye");
                mapControl2.Map.Layers.Insert(0, eagleEye);
            }
            catch (Exception)
            { }
        }

        private void mapControl2_MouseClick(object sender, MouseEventArgs e)
        {
            //鹰眼地图点击时切换主地图到该点中兴
            DPoint pt = new DPoint();
            mapControl2.Map.DisplayTransform.FromDisplay(e.Location, out pt);
            mapControl1.Map.Center = pt;

        }

        private void MainMap_Load(object sender, EventArgs e)
        {
            //加载主地图和鹰眼地图
            this.LoadMap();
        }

        private void LoadMap()
        {
            //加载地图
            string MapPath = Path.Combine(Application.StartupPath, @"map\map.mws");
            MapWorkSpaceLoader mwsLoader = new MapWorkSpaceLoader(MapPath);
            mapControl1.Map.Load(mwsLoader);
            mapControl2.Map = mapControl1.Map.Clone() as Map; //设置鹰眼地图
        }

        private void loadEagleLayer()
        {
            //初始化鹰眼图层 用户画框

            TableInfoMemTable ti = new TableInfoMemTable("EagleEyeTemp");
            ti.Temporary = true;
            Column column;
            column = new GeometryColumn(mapControl2.Map.GetDisplayCoordSys());
            column.Alias = "MI_Geometry ";
            column.DataType = MIDbType.FeatureGeometry;
            ti.Columns.Add(column);

            column = new Column();
            column.Alias = "MI_Style ";
            column.DataType = MIDbType.Style;
            ti.Columns.Add(column);
            Table table;
            try
            {
                table = Session.Current.Catalog.CreateTable(ti);

            }
            catch (Exception ex)
            {
                table = Session.Current.Catalog.GetTable("EagleEyeTemp");
            }
            if (mapControl2.Map.Layers["MyEagleEye"] != null)
                mapControl2.Map.Layers.Remove(eagleEye);
            eagleEye = new FeatureLayer(table, "EagleEye ", "MyEagleEye");
            mapControl2.Map.Layers.Insert(0, eagleEye);
            mapControl1.Refresh();
        }

        private void mapToolBar1_ButtonClick(object sender, ToolBarButtonClickEventArgs e)
        {
            switch (e.Button.Name)
            {
                case "toolBarEagle":
                    //打开关闭鹰眼
                    pEagle.Visible = !pEagle.Visible;
                    break;
                case "toolBarSearch":
                    //加载查找对话框获取查找条件
                    SearcgWorksForm frmSearcgWorksForm = new SearcgWorksForm();
                    if (frmSearcgWorksForm.ShowDialog() == DialogResult.OK)
                    {
                        //执行查找
                        Search(frmSearcgWorksForm.SearchWords);
                    }
                    break;
                default:
                    break;
            }
        }

        private void Search(string SearchWords)
        {
            //按照关键字查找图元
            Session.Current.Selections.DefaultSelection.Clear();
            bool bFind = false;

            //遍历图层
            double minx = double.MaxValue;
            double miny = double.MaxValue;
            double maxx = double.MinValue;
            double maxy = double.MinValue;
            foreach (IMapLayer layer in this.mapControl1.Map.Layers)
            {
                if (layer is FeatureLayer)
                {
                    Table tLayer = (layer as FeatureLayer).Table;
                    if (tLayer != null)
                    {
                        //生成查询条件 表中所有字段匹配即可
                        string Where = "0=1";
                        foreach (Column c in (layer as FeatureLayer).Table.TableInfo.Columns)
                        {
                            if ("Obj".Equals(c.Alias) || "MI_Style".Equals(c.Alias))
                                continue;
                            Where += " or " + c.Alias + " like '%" + SearchWords + "%'";
                        }
                        //查找
                        SearchInfo si = MapInfo.Data.SearchInfoFactory.SearchWhere(Where);
                        IResultSetFeatureCollection ifs = MapInfo.Engine.Session.Current.Catalog.Search(tLayer, si);
                        if (ifs.Count > 0)
                        {
                            //查询结果选中并定位到地图中兴
                            Session.Current.Selections.DefaultSelection.Add(ifs);
                            //this.mapControl1.Map.Center = ifs[0].Geometry.Centroid;
                            bFind = true;
                        }

                        foreach (Feature f in ifs)
                        {
                            if (f.Geometry.Centroid.x > maxx)
                            {
                                maxx = f.Geometry.Centroid.x;
                            }

                            if (f.Geometry.Centroid.y > maxy)
                            {
                                maxy = f.Geometry.Centroid.y;
                            }

                            if (f.Geometry.Centroid.x < minx)
                            {
                                minx = f.Geometry.Centroid.x;
                            }

                            if (f.Geometry.Centroid.y < miny)
                            {
                                miny = f.Geometry.Centroid.y;
                            }
                        }
                    }
                }
            }
            if (!bFind)
            {
                MessageBox.Show("未找到查找的内容！");
            }
            else
            {
                if (minx == maxx && miny == maxy)
                {
                    this.mapControl1.Map.Center = new DPoint(minx, miny); ;
                }
                else
                {
                    this.mapControl1.Map.Bounds = new DRect(minx, miny, maxx, maxy);
                }
            }
        }

        private void MainMap_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }
    }
}