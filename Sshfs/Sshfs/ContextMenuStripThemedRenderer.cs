using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Sshfs
{
   internal sealed class ContextMenuStripThemedRenderer : ToolStripProfessionalRenderer
    {

        private static bool IsSupported
        {
            get
            {
                return VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(VisualStyleElement.CreateElement("menu", 7, 1));
            }
        }



          private static int GetItemState(ToolStripItem item)
          {
           
              return item.Enabled ? (item.Selected ? 2 : 1) : (item.Selected ? 4 : 3);
          }
        


        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.IsOnDropDown && IsSupported)
            {

                var renderer = new VisualStyleRenderer("menu", 14, GetItemState(e.Item));
                
                e.TextColor = renderer.GetColor(ColorProperty.TextColor);

            }

            base.OnRenderItemText(e);

        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip.IsDropDown && IsSupported)
            {

                var renderer = new VisualStyleRenderer("menu", 10, 0);
                var clip = e.Graphics.Clip;
                var clientRectangle = e.ToolStrip.ClientRectangle;
                clientRectangle.Inflate(-1, -1);
                e.Graphics.ExcludeClip(clientRectangle);
                renderer.DrawBackground(e.Graphics, e.ToolStrip.ClientRectangle, e.AffectedBounds);
                e.Graphics.Clip = clip;


            }
            else
            {
                base.OnRenderToolStripBorder(e);
            }

        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.IsOnDropDown && IsSupported)
            {

                var renderer = new VisualStyleRenderer("menu", 14, GetItemState(e.Item));

                var backgroundRectangle = new Rectangle(e.Item.ContentRectangle.X + 1, 0, e.Item.ContentRectangle.Width - 1, e.Item.Bounds.Height);
                renderer.DrawBackground(e.Graphics, backgroundRectangle, backgroundRectangle);

            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip.IsDropDown && IsSupported)
            {

                var renderer = new VisualStyleRenderer("menu", 9, 0);

                if (renderer.IsBackgroundPartiallyTransparent())
                {
                    renderer.DrawParentBackground(e.Graphics, e.ToolStrip.ClientRectangle, e.ToolStrip);
                }

                renderer.DrawBackground(e.Graphics, e.ToolStrip.ClientRectangle, e.AffectedBounds);


            }
            else
            {
                base.OnRenderToolStripBackground(e);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            if (e.ToolStrip.IsDropDown && IsSupported)
            {
                var renderer = new VisualStyleRenderer("menu", 15, 0);
                var backgroundRectangle = new Rectangle(e.ToolStrip.DisplayRectangle.Left, 0, e.ToolStrip.DisplayRectangle.Width, e.Item.Height);
                renderer.DrawBackground(e.Graphics, backgroundRectangle, backgroundRectangle);
            }
            else
            {
                base.OnRenderSeparator(e);
            }

        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            if (e.Item.IsOnDropDown && IsSupported)
            {

                var renderer = new VisualStyleRenderer("menu", 12, e.Item.Enabled ? 2 : 1);

                var bounds = new Rectangle(e.Item.ContentRectangle.X + 1, 0, e.Item.Bounds.Height, e.Item.Bounds.Height);

                if (e.Item.RightToLeft == RightToLeft.Yes)
                    bounds = new Rectangle(e.ToolStrip.ClientSize.Width - bounds.X - bounds.Width, bounds.Y, bounds.Width, bounds.Height);

                renderer.DrawBackground(e.Graphics, bounds);

                var imageRectangle = e.ImageRectangle;

                imageRectangle.X = bounds.X + bounds.Width / 2 - imageRectangle.Width / 2;
                imageRectangle.Y = bounds.Y + bounds.Height / 2 - imageRectangle.Height / 2;

                renderer.SetParameters("menu", 11, e.Item.Enabled ? 1 : 2);

                renderer.DrawBackground(e.Graphics, imageRectangle);

            }
            else
            {
                base.OnRenderItemCheck(e);
            }
        }

      /*  protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip.IsDropDown && IsSupported)
            {
                var renderer = new VisualStyleRenderer("menu", 13, 0);


                var themeMargins = renderer.GetMargins(e.Graphics, MarginProperty.CaptionMargins);

                themeMargins.Right+=2;

                int num = e.ToolStrip.Width - e.ToolStrip.DisplayRectangle.Width - themeMargins.Left - themeMargins.Right - 1 - e.AffectedBounds.Width;
                var bounds = e.AffectedBounds;
                bounds.Y += 2;
                bounds.Height -= 4;
                int width = renderer.GetPartSize(e.Graphics, ThemeSizeType.True).Width;
                if (e.ToolStrip.RightToLeft == RightToLeft.Yes)
                {
                    bounds = new Rectangle(bounds.X - num, bounds.Y, width, bounds.Height);
                    bounds.X += width;
                }
                else
                {
                    bounds = new Rectangle(bounds.Width + num - width, bounds.Y, width, bounds.Height);
                }
                renderer.DrawBackground(e.Graphics, bounds);
            }
            else
            {
                base.OnRenderImageMargin(e);
            }
        }*/


    }
}
