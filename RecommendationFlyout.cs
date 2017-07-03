using DevComponents.DotNetBar.Controls;
using Light.Interface.Controls.Static;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static CrossSale_Forms_Test1.CrossSale;

namespace CrossSale_Forms_Test1
{
    class RecommendationFlyout
    {
        public struct ProductInfo
        {
            public Image ProductIcon;
            public string Name;
            public string Description;
            public double Certainty;
            public Guid OriginalProductID;
            public Guid RecommendedProductID;
        }
        
        //  Coefficient for Alpha in RGBA color format
        // alpha = (255 - alphaCoefficient) + alphaCoefficient * certainty
        //  The more this coef, the more difference between text transparency,
        // for example, for certainties 0.9 and 0.7
        //  Min - 0, max - 255
        private static int alphaCoefficient = 255;
        
        // Width and height of the product image
        private static int pictureWidth = 64;
        private static int pictureHeight = 64;

        // Width of the label with name of the product
        // Description label width = pictureWidth + labelNameWidth
        private static int labelNameWidth = 300;

        // Width of the button, that adds recommended product to the check
        private static int buttonAddWidth = 150;

        // Control, on which all recommendations will be shown
        private static Flyout flyout;

        // Needed for report about success or failure of recommendations
        private static Dictionary<ProductPair, bool> report;

        private static Color flyoutBackColor = Color.Orange;
        private static Color recommendationBackColor = Color.AliceBlue;

        private static bool isClosed = true;
        private static bool isHidden = true;

        public static bool IsClosed {
            get {
                return isClosed;
            }
            private set {
                isClosed = value;
            }
        }

        public static bool IsHidden {
            get {
                return isHidden;
            }
            private set {
                isHidden = value;
            }
        }

        /// <summary>
        /// Shows flyout(tooltip) with recommended products
        /// </summary>
        /// <param name="targetControl">The control, that will be pointed out by flyout</param>
        /// <param name="clientSex">client's sex: "Male" / "Female"</param>
        /// <param name="recommendedProducts">array of recommended products</param>
        /// <param name="addRecommendationToTheCheck">method, that will receive ProductInfo of recommended product to add that product to the check</param>
        public static void ShowRecommendations(Control targetControl, string clientSex, ProductInfo[] recommendedProducts, Action<ProductInfo> addRecommendationToTheCheck) {

            bool targetControl_isNull = targetControl == null;
            bool clientSex_isValid = (clientSex == "Male") || (clientSex == "Female");
            bool recommendedProducts_isNull = recommendedProducts == null;
            bool inputAction_isNull = addRecommendationToTheCheck == null;

            if (targetControl_isNull || !clientSex_isValid || recommendedProducts_isNull || inputAction_isNull) {
                return;
            }

            IsClosed = false;
            IsHidden = false;

            report = new Dictionary<ProductPair, bool>(recommendedProducts.Length);

            if (flyout != null)
                flyout.Close();

            // Main layout width all recommendations
            TableLayoutPanel mainLayout = new TableLayoutPanel {
                ColumnCount = 1,
                RowCount = recommendedProducts.Length + 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            mainLayout.Controls.Clear();
            
            // Title of flyout - contains title label and button to close flyout
            TableLayoutPanel title = new TableLayoutPanel {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly
            };

            title.ColumnStyles.Clear();
            title.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 90.45f));
            title.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.55f));

            // Adding label to the title ...
            title.Controls.Add(new LightLabel {
                Location = new Point(1, 1),
                Size = new Size(180, 30),
                AutoHeight = true,
                Text = "Рекомендации",
                Font = new Font(FontFamily.GenericSansSerif, 18, FontStyle.Bold),
                ForeColor = Color.Black
            });

            // ... and button to close flyout
            LightButton buttonClose = new LightButton {
                Location = new Point(1, 1),
                Size = new Size(30, 30),
                Text = "X",
                Font = new Font(FontFamily.GenericSansSerif, 15, FontStyle.Bold)
            };
            title.Controls.Add(buttonClose);

            mainLayout.Controls.Add(title);

            // Adding information for all recommended products
            foreach (var pi in recommendedProducts) {

                bool nameIsNullOrEmpty = String.IsNullOrEmpty(pi.Name);
                bool originalProductIsEmpty = pi.OriginalProductID.Equals(Guid.Empty);
                bool recommendedProductIsEmpty = pi.RecommendedProductID.Equals(Guid.Empty);
                if (nameIsNullOrEmpty || originalProductIsEmpty || recommendedProductIsEmpty) {
                    continue;
                }

                ProductPair productPair = new ProductPair(pi.OriginalProductID, pi.RecommendedProductID);
                if (!report.ContainsKey(productPair)) {
                    report.Add(productPair, false);

                    var newElement = InitNewTableLayout(pi, addRecommendationToTheCheck);

                    mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    mainLayout.Controls.Add(newElement);
                }
            }

            // Initializing flyout
            flyout = new Flyout {
                ActivateOnShow = true,
                Content = mainLayout,
                BackColor = flyoutBackColor,
                CloseMode = eFlyoutCloseMode.Manual,
                DisplayMode = eFlyoutDisplayMode.Manual,
                TargetControl = (Control)targetControl,
                PointerSide = ePointerSide.Left
            };
            
            // Adding event on 'X' button to close flyout
            buttonClose.Click += new EventHandler(delegate {
                Hide();
            });

            // Removing disposed controls
            mainLayout.Resize += new EventHandler(delegate {
                for (int i = 0; i < mainLayout.Controls.Count; i++) {
                    if (mainLayout.Controls[i].IsDisposed) {
                        mainLayout.Controls.RemoveAt(i);
                        i--;
                    }
                }
                if (mainLayout.Controls.Count <= 1) {
                    Close();
                }
            });

            // Reporting about successes and failures on flyout closed
            flyout.FlyoutClosed += new FormClosedEventHandler(delegate {
                if (IsClosed) {
                    CrossSale.ReportSuccess(clientSex, report);
                }
            });


            flyout.Show();
            flyout.Close();
            flyout.Show();

        }

        /// <summary>
        /// Initializes new TableLayoutPanel with the information about recommended product
        /// </summary>
        /// <param name="productInfo">information about recommended product</param>
        /// <param name="addRecommendationToTheCheck">response action, that will be called on button click</param>
        /// <returns></returns>
        private static TableLayoutPanel InitNewTableLayout(ProductInfo productInfo, Action<ProductInfo> addRecommendationToTheCheck)
        {
            // Product Image
            PictureBox productPicture = new PictureBox {
                Image = productInfo.ProductIcon,
                Size = new Size(pictureWidth, pictureHeight),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BorderStyle = BorderStyle.FixedSingle
            };

            // All about content font
            Font font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold);
            int alpha = (int)((255 - alphaCoefficient) + alphaCoefficient * productInfo.Certainty);
            Color fontColor = Color.FromArgb(alpha, Color.Black);

            // Label with product name
            LightLabel labelProductName = new LightLabel {
                Location = new Point(1, 1),
                Size = new Size(labelNameWidth, 50),
                Font = font,
                ForeColor = fontColor,
                Text = productInfo.Name,
                AutoHeight = true
            };

            // A little bit smaller font for description and button
            font = new Font(FontFamily.GenericSansSerif, 10);

            // Label with description for product
            LightLabel labelDescription = new LightLabel {
                Location = new Point(1, 1),
                Font = font,
                ForeColor = fontColor,
                Text = String.IsNullOrEmpty(productInfo.Description) ? " " : productInfo.Description,
                Width = pictureWidth + labelNameWidth,
                MinimumSize = new Size(364, 30),
                MaximumSize = new Size(364, 0),
                AutoSize = false,
                AutoHeight = true
            };

            // Self-describing name
            LightButton buttonAdd = new LightButton {
                Location = new Point(1, 1),
                Size = new Size(buttonAddWidth, 30),
                Font = new Font(FontFamily.GenericSansSerif, 14),
                Text = "Добавить в чек",
                TextColor = fontColor,
                Anchor = AnchorStyles.Right
            };

            // Needed to group up image, labelName, labelDescription and button
            TableLayoutPanel table = new TableLayoutPanel {
                ColumnCount = 2,
                RowCount = 3,
                Width = productPicture.Width + labelProductName.Width + 5,
                Height = productPicture.Height + labelDescription.Height + buttonAdd.Height + 35,
                Left = 5,
                Top = 5,
                BackColor = recommendationBackColor,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            // Setting up TableLayoutPanel columns and rows
            table.ColumnStyles.Clear();
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            table.RowStyles.Clear();
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Adding controls
            table.Controls.Add(productPicture, 0, 0);
            table.Controls.Add(labelProductName, 1, 0);
            table.Controls.Add(labelDescription, 1, 1);
            table.Controls.Add(buttonAdd, 1, 3);

            // labelDescription needs two columns
            table.SetColumnSpan(labelDescription, 2);

            // On buttonAdd click - set up report and adding recommended product to the check
            buttonAdd.Click += new EventHandler(delegate {
                ProductPair pair = new ProductPair(productInfo.OriginalProductID, productInfo.RecommendedProductID);
                report[pair] = true;

                addRecommendationToTheCheck(productInfo);
                table.Dispose();
            });
            
            return table;
        }

        /// <summary>
        /// Redraw flyout
        /// </summary>
        public static void ReShow() {
            if (!isClosed) {
                isHidden = false;
                flyout.Close();
                flyout.Show();
            }
        }
        
        /// <summary>
        /// Closing shown flyout and sending report about successes or failures of recommendations
        /// </summary>
        public static void Close()
        {
            if (!isClosed) {
                isClosed = true;
                IsHidden = true;
                flyout.Close();
                flyout = null;
            }
        }

        /// <summary>
        /// Hides shown flyout, but the content inside it will not be deleted and so report will not be sent.
        /// To show flyout again with the same content, use ReShow function
        /// </summary>
        public static void Hide()
        {
            if (!isHidden) {
                isHidden = true;
                flyout.Close();
            }
        }
    }
}
