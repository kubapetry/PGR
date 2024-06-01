using System;
using Gtk;
//using System.IO;
using Cairo;
using System.Collections.Generic;

namespace grafika2
{



    public class SimplePaint : Window
    {
        private DrawingArea drawingArea;
        private bool isDrawing = false;
        private Gdk.Point lastPoint;
        private Gdk.Point startPoint;
        private Gdk.Point endPoint;
        private Color currentColor = new Color(0, 0, 0); // Výchozí barva je černá
        private double lineWidth = 2; // Výchozí tloušťka čáry
        private double lineOpacity = 1;
        private bool isErasing = false; // Režim gumování
        private bool isPenMode = false; // Režim kreslení
        private bool isFilling = false;
        private bool isAntialiasing = false;
        private bool isLine = false;
        private bool isRectangle = false;
        private bool isCircle = false;
        private bool isTriangle = false;
        private bool isDrawingLine = false;
        private ImageSurface surface;
        private double zoomLevel = 1.0;



        public SimplePaint() : base("Simple Paint")
        {
            SetDefaultSize(800, 600);
            SetPosition(WindowPosition.Center);

            drawingArea = new DrawingArea();
            drawingArea.AddEvents((int)Gdk.EventMask.ButtonPressMask |
                                  (int)Gdk.EventMask.ButtonReleaseMask |
                                  (int)Gdk.EventMask.PointerMotionMask);
            drawingArea.ExposeEvent += OnExpose;
            drawingArea.ButtonPressEvent += OnButtonPress;
            drawingArea.ButtonReleaseEvent += OnButtonRelease;
            drawingArea.MotionNotifyEvent += OnMouseMove;
            drawingArea.SizeAllocated += OnSizeAllocated;

            CreateUI();
            InitializeSurface();
            ShowAll();
        }

        void CreateUI()
        {

            VBox vbox = new VBox(false, 2);

            HBox hbox = new HBox(false, 2);

            HBox toolbar = new HBox(false, 2);

            Button colorButton = new Button("Choose Color");
            colorButton.Clicked += OnChooseColorClicked;

            Button penButton = new Button("Pen");
            penButton.Clicked += OnPenClicked;

            Button eraseButton = new Button("Eraser");
            eraseButton.Clicked += OnEraserClicked;

            Button fillButton = new Button("Fill");
            fillButton.Clicked += OnFillClicked;

            SpinButton lineWidthSpinner = new SpinButton(1, 10, 1);
            lineWidthSpinner.ValueChanged += (sender, e) => { lineWidth = lineWidthSpinner.Value; };

            Button opacityButton = new Button("Opacity");
            opacityButton.Clicked += OnOpacityClicked;

            Button saveButton = new Button("Save");
            saveButton.Clicked += OnSaveClicked;

            Button loadButton = new Button("Load");
            loadButton.Clicked += OnLoadClicked;

            Button zoomInButton = new Button("+");
            zoomInButton.Clicked += OnZoomInClicked;

            Button zoomOutButton = new Button("-");
            zoomOutButton.Clicked += OnZoomOutClicked;

            Button antiAliasingOn = new Button("Anti aliasing On");
            antiAliasingOn.Clicked += (sender, e) => { isAntialiasing = true; };

            Button antiAliasingOff = new Button("Anti aliasing Off");
            antiAliasingOff.Clicked += (sender, e) => { isAntialiasing = false; };

            Button lineButton = new Button("Line");
            lineButton.Clicked += OnLineClicked;

            Button rectangleButton = new Button("Rectangle");
            rectangleButton.Clicked += OnRectangleClicked;

            Button circleButton = new Button("Circle");
            circleButton.Clicked += OnCircleClicked;

            Button triangleButton = new Button("Triangle");
            triangleButton.Clicked += OnTriangleClicked;


            hbox.PackStart(colorButton, false, false, 0);
            hbox.PackStart(penButton, false, false, 0);
            hbox.PackStart(eraseButton, false, false, 0);
            hbox.PackStart(fillButton, false, false, 0);
            hbox.PackStart(new Label("Line Width:"), false, false, 0);
            hbox.PackStart(lineWidthSpinner, false, false, 0);
            hbox.PackStart(opacityButton, false, false, 0);
            hbox.Add(opacityButton);
            hbox.PackStart(saveButton, false, false, 0);
            hbox.PackStart(loadButton, false, false, 0);
            toolbar.PackStart(zoomInButton, false, false, 0);
            toolbar.PackStart(zoomOutButton, false, false, 0);
            toolbar.PackStart(antiAliasingOn, false, false, 0);
            toolbar.PackStart(antiAliasingOff, false, false, 0);
            toolbar.PackStart(lineButton, false, false, 0);
            toolbar.PackStart(rectangleButton, false, false, 0);
            toolbar.PackStart(circleButton, false, false, 0);
            toolbar.PackStart(triangleButton, false, false, 0);


            vbox.PackStart(hbox, false, false, 0);
            vbox.PackStart(toolbar, false, false, 0);
            vbox.PackStart(drawingArea, true, true, 0);


            Add(vbox);


        }


        void InitializeSurface()
        {
            
            surface = new ImageSurface(Format.Argb32, drawingArea.Allocation.Width, drawingArea.Allocation.Height);
            using (Context cr = new Context(surface))
            {
                cr.SetSourceRGB(1, 1, 1); // Bílé pozadí
                cr.Paint();
            }
        }

        void OnExpose(object sender, ExposeEventArgs args)
        {
            using (Context cr = Gdk.CairoHelper.Create(drawingArea.GdkWindow))
            {
                cr.Scale(zoomLevel, zoomLevel);
                cr.SetSourceSurface(surface, 0, 0);
                cr.Paint();

                if (isDrawing && isDrawingLine)
                {
                    cr.Antialias = isAntialiasing ? Antialias.Subpixel : Antialias.None;
                    cr.LineWidth = lineWidth;
                    cr.SetSourceColor(currentColor);

                    if (isLine)
                    {
                        cr.MoveTo(startPoint.X / zoomLevel, startPoint.Y / zoomLevel);
                        cr.LineTo(endPoint.X / zoomLevel, endPoint.Y / zoomLevel);
                        cr.Stroke();
                    }
                    else if (isRectangle)
                    {
                        double x = Math.Min(startPoint.X, endPoint.X) / zoomLevel;
                        double y = Math.Min(startPoint.Y, endPoint.Y) / zoomLevel;
                        double width = Math.Abs(endPoint.X - startPoint.X) / zoomLevel;
                        double height = Math.Abs(endPoint.Y - startPoint.Y) / zoomLevel;
                        cr.Rectangle(x, y, width, height);
                        cr.Stroke();
                    }
                    else if (isCircle)
                    {
                        double radius = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2)) / zoomLevel;
                        cr.Arc(startPoint.X / zoomLevel, startPoint.Y / zoomLevel, radius, 0, 2 * Math.PI);
                        cr.Stroke();
                    }
                    else if (isTriangle)
                    {
                        cr.MoveTo(startPoint.X / zoomLevel, startPoint.Y / zoomLevel);
                        cr.LineTo(endPoint.X / zoomLevel, startPoint.Y / zoomLevel);
                        cr.LineTo((startPoint.X + endPoint.X) / 2 / zoomLevel, endPoint.Y / zoomLevel);
                        cr.ClosePath();
                        cr.Stroke();
                    }
                }
            }
        }



        void OnPenClicked(object sender, EventArgs e)
        {
            isErasing = false; // Režim gumování
            isPenMode = true; // Režim kreslení
            isFilling = false; // Vypnutí režimu vyplňování
            isLine = false;
            isRectangle = false;
            isCircle = false;
            isTriangle = false;
        }

        void OnEraserClicked(object sender, EventArgs e)
        {
            isErasing = true; // Režim gumování
            isPenMode = false; // Režim kreslení
            isFilling = false; // Vypnutí režimu vyplňování
            isLine = false;
            isRectangle = false;
            isCircle = false;
            isTriangle = false;
        }

        void OnFillClicked(object sender, EventArgs e)
        {
            isErasing = false;
            isPenMode = false;
            isFilling = true;
            isLine = false;
            isRectangle = false;
            isCircle = false;
            isTriangle = false;
        }

        void OnLineClicked(object sender, EventArgs e)
        {
            isErasing = false;
            isPenMode = false;
            isFilling = false;
            isLine = true;
            isRectangle = false;
            isCircle = false;
            isTriangle = false;
        }

        void OnRectangleClicked(object sender, EventArgs e)
        {
            isErasing = false;
            isPenMode = false;
            isFilling = false;
            isLine = false;
            isRectangle = true;
            isCircle = false;
            isTriangle = false;
        }

        void OnCircleClicked(object sender, EventArgs e)
        {
            isErasing = false;
            isPenMode = false;
            isFilling = false;
            isLine = false;
            isRectangle = false;
            isCircle = true;
            isTriangle = false;
        }

        void OnTriangleClicked(object sender, EventArgs e)
        {
            isErasing = false;
            isPenMode = false;
            isFilling = false;
            isLine = false;
            isRectangle = false;
            isCircle = false;
            isTriangle = true; // Nastavit režim kreslení trojúhelníku
        }

        void OnZoomInClicked(object sender, EventArgs e)
        {
            zoomLevel *= 1.1; // Increase zoom level by 10%
            drawingArea.QueueDraw();
        }

        void OnZoomOutClicked(object sender, EventArgs e)
        {
            zoomLevel /= 1.1; // Decrease zoom level by 10%
            drawingArea.QueueDraw();
        }

        void OnOpacityClicked(object sender, EventArgs e)
        {
            // Vytvoření dialogového okna pro zadání hodnoty průhlednosti
            Dialog dialog = new Dialog("Set Opacity", this, DialogFlags.Modal);
            dialog.SetDefaultSize(200, 100);

            Label label = new Label("Enter opacity (0.0 to 1.0):");
            Entry entry = new Entry();
            entry.Text = lineOpacity.ToString();

            Button okButton = new Button("OK");
            okButton.Clicked += (s, args) => 
            {
                if (double.TryParse(entry.Text, out double newOpacity))
                {
                    if (newOpacity >= 0.0 && newOpacity <= 1.0)
                    {
                        lineOpacity = newOpacity;

                    }
                    else
                    {
                        MessageDialog md = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Opacity must be between 0.0 and 1.0");
                        md.Run();
                        md.Destroy();
                    }
                }
                else
                {
                    MessageDialog md = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Invalid input. Please enter a number.");
                    md.Run();
                    md.Destroy();
                }
                dialog.Destroy();

            };

            dialog.VBox.PackStart(label, false, false, 0);
            dialog.VBox.PackStart(entry, false, false, 0);
            dialog.ActionArea.PackStart(okButton, false, false, 0);

            dialog.ShowAll();
        }


        void OnButtonPress(object sender, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 1) // Levé tlačítko myši
            {
                isDrawing = true;
                startPoint = new Gdk.Point((int)args.Event.X, (int)args.Event.Y);
                lastPoint = startPoint;
                if (isLine || isRectangle || isCircle || isTriangle)
                {
                    isDrawingLine = true;
                    endPoint = startPoint; // Inicializace koncového bodu na startPoint
                }
                else if (isFilling)
                {
                    FloodFill(startPoint.X, startPoint.Y, currentColor);
                    drawingArea.QueueDraw(); // Požádat o překreslení plátna
                }
            }
        }

        void OnButtonRelease(object sender, ButtonReleaseEventArgs args)
        {
            if (args.Event.Button == 1 && isDrawing) // Levé tlačítko myši
            {
                isDrawing = false;
                if (isDrawingLine)
                {
                    endPoint = new Gdk.Point((int)args.Event.X, (int)args.Event.Y);
                    using (Context cr = new Context(surface))
                    {
                        cr.Antialias = isAntialiasing ? Antialias.Subpixel : Antialias.None;
                        cr.LineWidth = lineWidth;
                        cr.SetSourceRGBA(currentColor.R, currentColor.G, currentColor.B, lineOpacity);

                        if (isLine)
                        {
                            cr.MoveTo(startPoint.X / zoomLevel, startPoint.Y / zoomLevel);
                            cr.LineTo(endPoint.X / zoomLevel, endPoint.Y / zoomLevel);
                            cr.Stroke();
                        }
                        else if (isRectangle)
                        {
                            double x = Math.Min(startPoint.X, endPoint.X) / zoomLevel;
                            double y = Math.Min(startPoint.Y, endPoint.Y) / zoomLevel;
                            double width = Math.Abs(endPoint.X - startPoint.X) / zoomLevel;
                            double height = Math.Abs(endPoint.Y - startPoint.Y) / zoomLevel;
                            cr.Rectangle(x, y, width, height);
                            cr.Stroke();
                        }
                        else if (isCircle)
                        {
                            double radius = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2)) / zoomLevel;
                            cr.Arc(startPoint.X / zoomLevel, startPoint.Y / zoomLevel, radius, 0, 2 * Math.PI);
                            cr.Stroke();
                        }
                        else if (isTriangle)
                        {
                            cr.MoveTo(startPoint.X / zoomLevel, startPoint.Y / zoomLevel);
                            cr.LineTo(endPoint.X / zoomLevel, startPoint.Y / zoomLevel);
                            cr.LineTo((startPoint.X + endPoint.X) / 2 / zoomLevel, endPoint.Y / zoomLevel);
                            cr.ClosePath();
                            cr.Stroke();
                        }


                        drawingArea.QueueDraw(); // Požádat o překreslení plátna
                    }
                    isDrawingLine = false;
                }
                else
                {
                    // Resetování posledního bodu po dokončení kreslení perem nebo gumování
                    lastPoint = new Gdk.Point((int)args.Event.X, (int)args.Event.Y);
                }
            }
        }

        void OnMouseMove(object sender, MotionNotifyEventArgs args)
        {
            if (isDrawing && isDrawingLine)
            {
                endPoint = new Gdk.Point((int)args.Event.X, (int)args.Event.Y);
                drawingArea.QueueDraw(); // Požádat o překreslení plátna
            }
            else if (isDrawing)
            {
                var newPoint = new Gdk.Point((int)args.Event.X, (int)args.Event.Y);
                using (Context cr = new Context(surface))
                {
                    cr.Antialias = isAntialiasing ? Antialias.Subpixel : Antialias.None;
                    cr.LineWidth = lineWidth;
                    if (isErasing)
                    {
                        cr.SetSourceRGB(1, 1, 1); // Nastavení operátoru na Clear pro gumování
                        cr.MoveTo(lastPoint.X / zoomLevel, lastPoint.Y / zoomLevel);
                        cr.LineTo(newPoint.X / zoomLevel, newPoint.Y / zoomLevel);
                        cr.Stroke();
                    }
                    else if (isPenMode)
                    {
                        cr.SetSourceRGBA(currentColor.R, currentColor.G, currentColor.B, lineOpacity);
                        cr.MoveTo(lastPoint.X / zoomLevel, lastPoint.Y / zoomLevel);
                        cr.LineTo(newPoint.X / zoomLevel, newPoint.Y / zoomLevel);
                        cr.Stroke();
                    }


                    lastPoint = newPoint;
                    drawingArea.QueueDraw();
                }

            }
        }

        void OnSizeAllocated(object sender, SizeAllocatedArgs args)
        {
            if (surface == null || surface.Width != args.Allocation.Width || surface.Height != args.Allocation.Height)
            {
                InitializeSurface();
            }
        }

        void OnSaveClicked(object sender, EventArgs e)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Save Image", this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
            fileChooser.DoOverwriteConfirmation = true;

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                string filename = fileChooser.Filename;
                surface.WriteToPng(filename);
            }

            fileChooser.Destroy();
        }

        void OnLoadClicked(object sender, EventArgs e)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Load Image", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                string filename = fileChooser.Filename;
                using (var pixbuf = new Gdk.Pixbuf(filename))
                {
                    using (Context cr = new Context(surface))
                    {
                        cr.SetSourceRGB(1, 1, 1); // Bílé pozadí
                        cr.Paint();
                        Gdk.CairoHelper.SetSourcePixbuf(cr, pixbuf, 0, 0);
                        cr.Paint();
                    }
                    drawingArea.QueueDraw(); // Požádat o překreslení plátna
                }
            }

            fileChooser.Destroy();
        }

        void OnChooseColorClicked(object sender, EventArgs e)
        {
            ColorSelectionDialog colorDialog = new ColorSelectionDialog("Choose Color");
            if (colorDialog.Run() == (int)ResponseType.Ok)
            {
                Gdk.Color gdkColor = colorDialog.ColorSelection.CurrentColor;
                currentColor = new Color(gdkColor.Red / 65535.0, gdkColor.Green / 65535.0, gdkColor.Blue / 65535.0);
                isErasing = false;
            }
            colorDialog.Destroy();
        }


        void FloodFill(int x, int y, Color fillColor)
        {
            var targetColor = GetPixelColor(x, y);
            if (AreColorsEqual(fillColor, targetColor)) return;

            Stack<Gdk.Point> pixels = new Stack<Gdk.Point>();
            pixels.Push(new Gdk.Point(x, y));

            while (pixels.Count > 0)
            {
                Gdk.Point point = pixels.Pop();
                int px = point.X;
                int py = point.Y;

                if (px < 0 || px >= surface.Width || py < 0 || py >= surface.Height)
                    continue;

                Color currentColor = GetPixelColor(px, py);

                if (AreColorsEqual(currentColor, targetColor))
                {

                    var blendedColor = BlendColors(currentColor, fillColor, lineOpacity);

                    SetPixelColor(px, py, blendedColor);

                    pixels.Push(new Gdk.Point(px + 1, py));
                    pixels.Push(new Gdk.Point(px - 1, py));
                    pixels.Push(new Gdk.Point(px, py + 1));
                    pixels.Push(new Gdk.Point(px, py - 1));
                }
            }
            surface.MarkDirty();
        }

        bool AreColorsEqual(Color c1, Color c2)
        {
            return Math.Abs(c1.R - c2.R) < 0.01 &&
                   Math.Abs(c1.G - c2.G) < 0.01 &&
                   Math.Abs(c1.B - c2.B) < 0.01 &&
                   Math.Abs(c1.A - c2.A) < 0.01;
        }

        Color GetPixelColor(int x, int y)
        {
            var data = surface.DataPtr;
            int stride = surface.Stride;
            int index = y * stride + x * 4;

            unsafe
            {
                byte* ptr = (byte*)data;
                byte b = ptr[index];
                byte g = ptr[index + 1];
                byte r = ptr[index + 2];
                byte a = ptr[index + 3];

                return new Color(r / 255.0, g / 255.0, b / 255.0, a / 255.0);
            }
        }

        void SetPixelColor(int x, int y, Color color)
        {
            var data = surface.DataPtr;
            int stride = surface.Stride;
            int index = y * stride + x * 4;

            unsafe
            {
                byte* ptr = (byte*)data;
                ptr[index] = (byte)(color.B * 255);
                ptr[index + 1] = (byte)(color.G * 255);
                ptr[index + 2] = (byte)(color.R * 255);
                ptr[index + 3] = (byte)(color.A * 255);
            }

            surface.MarkDirty();
        }

        Color BlendColors(Color backgroundColor, Color fillColor, double opacity)
        {
            double r = (fillColor.R * fillColor.A * opacity) + (backgroundColor.R * backgroundColor.A * (1 - fillColor.A * opacity));
            double g = (fillColor.G * fillColor.A * opacity) + (backgroundColor.G * backgroundColor.A * (1 - fillColor.A * opacity));
            double b = (fillColor.B * fillColor.A * opacity) + (backgroundColor.B * backgroundColor.A * (1 - fillColor.A * opacity));
            double a = fillColor.A * opacity + backgroundColor.A * (1 - fillColor.A * opacity);

            return new Color(r, g, b, a);
        }



        public static void Main()
        {
            Application.Init();
            new SimplePaint();
            Application.Run();
        }
    }


}