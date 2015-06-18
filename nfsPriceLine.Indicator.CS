using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator {
	[SameAsSymbol(true)]
	public class nfsPriceLine : IndicatorObject, IChartCustomDrawer {
		private Pen askPen, bidPen, pricePen;
        private DOMPrice[] Asks, Bids;
        private double LastPrice;
		// drawing update timer. A value of 0.2 is 5x per second.
		// Min value allowed by MC is 0.1 (10x/sec).
		private const double UpdateSpeedSecs = 0.2;

		[Input]
		public int barsBefore {get; set;}

		[Input]
		public int barsAfter {get; set;}
		
		[Input]
		public Color bidColor {get; set;}
		
		[Input]
		public Color askColor {get; set;}
		
		[Input]
		public Color priceColor {get; set;}
		
		[Input]
		public int lineWidth {get; set;}
		
		public nfsPriceLine(object _ctx):base(_ctx){
			bidColor = Color.Red;
			askColor = Color.Blue;
			priceColor = Color.Cyan;
			barsBefore = 5;
			barsAfter = 2;
			lineWidth = 2;
		}

		protected override void Create() {
            ChartCustomDraw.Register(this);
			askPen = new Pen(askColor, lineWidth);
			bidPen = new Pen(bidColor, lineWidth);
			pricePen = new Pen(priceColor, lineWidth);
			pricePen.DashStyle = DashStyle.Dash;
		}

		protected override void StartCalc() {
		}
		
		protected override void CalcBar() {
			// we only deal with the last bar
			if (!Bars.LastBarOnChart)
				return;
			ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(UpdateSpeedSecs));
		}
		
	    protected override void Destroy() {
			ChartCustomDraw.Unregister(this);
			askPen = bidPen = pricePen = null;
		}

		// Drawing is triggered here on a timer rather than in
		// CalcBar() because the bid and ask can change without CalcBar()
		// being called.
		protected override void OnRecalcLastBarAfterEvent() {
            if (Bars.DOM.Connected){
                var _changed = false;
				// confusing logic; necessary because Bids/Asks will be null
				// on the first pass through.
				DOMPrice _old_ask = new DOMPrice(double.NaN, double.NaN);
				DOMPrice _old_bid = new DOMPrice(double.NaN, double.NaN);
				double _old_last = double.NaN;
                lock (this){
					if (null == Asks) {
						_changed = true;
					} else {
	                    _old_ask = Asks[0];
	                    _old_bid = Bids[0];
						_old_last = LastPrice;
					}
                    Asks = Bars.DOM.Ask;
                    Bids = Bars.DOM.Bid;
					LastPrice = Bars.Close[0];
                    _changed = _changed || !((_old_ask == Asks[0]) &&
											(_old_bid == Bids[0]) &&
											(_old_last == LastPrice));
                }
				_changed = _changed || !(LastPrice == Bars.Close[0]);
                
                if (_changed)
                    ChartCustomDraw.ReDraw();
                ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(UpdateSpeedSecs));
            }
		}
		
        void IChartCustomDrawer.Draw(DrawContext context, EDrawPhases phase) {
			if (phase != EDrawPhases.Final) return;

			DOMPrice[] asks, bids;
			lock(this) {
				asks = Asks;
				bids = Bids;
			}
			//Output.WriteLine("asks len is {0}, bid len is {1}", asks.Length, bids.Length);

			PointF pricePt = context.Environment.ChartPoint2Point(
				new ChartPoint(Bars.Time[0], Bars.Close[0]));
			PointF rightEdge = context.Environment.ChartPoint2Point(
				new ChartPoint(Environment.RightScreenTime, Bars.Close[0]));
			PointF bidPt = context.Environment.ChartPoint2Point(
				new ChartPoint(Bars.LastBarTime, bids[0].Price));
			PointF askPt = context.Environment.ChartPoint2Point(
				new ChartPoint(Bars.LastBarTime, asks[0].Price));

			int barWidth = (int) Environment.BarSpacing;
			
			context.graphics.DrawLine(pricePen, pricePt, rightEdge);
			context.graphics.DrawLine(bidPen, bidPt.X - barsBefore * barWidth, bidPt.Y,
				bidPt.X + barsAfter * barWidth, bidPt.Y);
			context.graphics.DrawLine(askPen, askPt.X - barsBefore * barWidth, askPt.Y,
				askPt.X + barsAfter * barWidth, askPt.Y);
		}
	}
}
