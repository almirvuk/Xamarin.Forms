using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Android.Content;
using Android.Views;
using AImageView = Android.Widget.ImageView;
using Xamarin.Forms.Internals;

// TODO GIF
namespace Xamarin.Forms.Platform.Android
{
	internal interface IImageRendererController
	{
		void SkipInvalidate();
		bool IsDisposed { get; }
	}

	public class ImageRenderer : ViewRenderer<Image, AImageView>
	{
		bool _isDisposed;
		readonly MotionEventHelper _motionEventHelper = new MotionEventHelper();

		public ImageRenderer(Context context) : base(context)
		{
			AutoPackage = false;
		}

		[Obsolete("This constructor is obsolete as of version 2.5. Please use ImageRenderer(Context) instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public ImageRenderer()
		{
			AutoPackage = false;
		}

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (Control != null)
			{
				if (Control.Drawable is FormsAnimationDrawable animation)
					animation.AnimationStopped -= OnAnimationStopped;

				Control.Reset();
			}

			_isDisposed = true;

			base.Dispose(disposing);
		}

		protected override AImageView CreateNativeControl()
		{
			return new FormsImageView(Context);
		}

		protected override async void OnElementChanged(ElementChangedEventArgs<Image> e)
		{
			base.OnElementChanged(e);

			if (e.OldElement == null)
			{
				var view = CreateNativeControl();
				SetNativeControl(view);
			}

			_motionEventHelper.UpdateElement(e.NewElement);

			await TryUpdateBitmap(e.OldElement);

			UpdateAspect();
		}

		protected override async void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Image.SourceProperty.PropertyName)
				await TryUpdateBitmap();
			else if (e.PropertyName == Image.AspectProperty.PropertyName)
				UpdateAspect();
			else if (e.PropertyName == Image.IsAnimationPlayingProperty.PropertyName)
				StartStopAnimation();
		}

		void UpdateAspect()
		{
			if (Element == null || Control == null || Control.IsDisposed())
			{
				return;
			}

			AImageView.ScaleType type = Element.Aspect.ToScaleType();
			Control.SetScaleType(type);
		}

		protected virtual async Task TryUpdateBitmap(Image previous = null)
		{
			// By default we'll just catch and log any exceptions thrown by UpdateBitmap so they don't bring down
			// the application; a custom renderer can override this method and handle exceptions from
			// UpdateBitmap differently if it wants to

			try
			{
				await UpdateBitmap(previous);
			}
			catch (Exception ex)
			{
				Log.Warning(nameof(ImageRenderer), "Error loading image: {0}", ex);
			}
			finally
			{
				((IImageController)Element)?.SetIsLoading(false);
			}
		}

		protected async Task UpdateBitmap(Image previous = null)
		{
			if (Element == null || Control == null || Control.IsDisposed())
			{
				return;
			}

			await Control.UpdateBitmap(Element, previous).ConfigureAwait(false);
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			if (base.OnTouchEvent(e))
				return true;

			return _motionEventHelper.HandleMotionEvent(Parent, e);
		}

		void StartStopAnimation()
		{
			if (_isDisposed || Element == null || Control == null)
			{
				return;
			}

			if (Element.IsLoading)
				return;

			if (Control.Drawable is FormsAnimationDrawable animation)
			{
				if (Element.IsAnimationPlaying && !animation.IsRunning)
					animation.Start();
				else if (!Element.IsAnimationPlaying && animation.IsRunning)
					animation.Stop();
			}
		}
	}
}