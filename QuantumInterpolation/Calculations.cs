using System;
using System.Drawing;

namespace QuantumInterpolation
{
    public static class Calculations
    {
        public static double Magnitude(this Vector<double> vector)
        {
            return vector.Dot(vector);
        }

        public static Vector<double> Plus(this Vector<double> left, Vector<double> right)
        {
            return new Vector<double>
            {
                x = left.x + right.x,
                y = left.y + right.y
            };
        }

        public static Vector<double> Along(this Vector<double> vector, Vector<double> direction)
        {
            return direction.Scale(vector.Dot(direction));
        } 

        public static Vector<double> Negative(this Vector<double> vector)
        {
            return vector.Fmap(q => -q);
        }

        public static Vector<double> Unit(this Vector<double> vector)
        {
            return vector.Scale(1 / Math.Sqrt(vector.Magnitude()));
        }

        public static Vector<double> Scale(this Vector<double> vector, double scalar)
        {
            return vector.Fmap(q => q * scalar);
        } 

        public static Vector<double> Minus(this Vector<double> left, Vector<double> right)
        {
            return left.Plus(right.Negative());
        } 

        public static double Square(this double number)
        {
            return number*number;
        }

        public static double Dot(this Vector<double> left, Vector<double> right)
        {
            return (left.x * right.x) + (left.y * right.y);
        }

        public static int Size(this Range<int> range)
        {
            return range.end - range.start;
        }
        
        public static double Size(this Range<double> range)
        {
            return range.end - range.start;
        }

        public static bool Contains<T>(this Range<T> range, T element)
            where T : IComparable<T>
        {
            return (range.start.CompareTo(element) < 0) && (element.CompareTo(range.end) < 0);
        }

        public static int Interpolate(this Range<int> range, double f)
        {
            return (int)Math.Round((1 - f) * range.start + f * range.end);
        }

        public static double Interpolate(this Range<double> range, double f)
        {
            return (1 - f) * range.start + f * range.end;
        }

        public static T Max<T>(this T left, T right)
            where T : IComparable<T>
        {
            return left.CompareTo(right) < 0 ? right : left;
        }
        
        public static T Min<T>(this T left, T right)
            where T : IComparable<T>
        {
            return left.CompareTo(right) < 0 ? left : right;
        }

        public static Range<T> Union<T>(this Range<T> left, Range<T> right)
            where T : IComparable<T>
        {
            if (left.Intersects(right))
            {
                return left.Span(right);
            }

            return null;
        }

        public static Range<Range<T>> Split<T>(this Range<T> range, T element)
        {
            return new Range<Range<T>>
            {
                start = new Range<T>
                {
                    start = range.start,
                    end = element,
                },
                end = new Range<T>
                {
                    start = element,
                    end = range.end,
                },
            };
        }  

        public static Range<T> Span<T>(this Range<T> left, Range<T> right)
            where T : IComparable<T>
        {
            return new Range<T>
            {
                start = left.start.Min(right.start),
                end = left.end.Max(right.end)
            };
        }

        public static bool Intersects<T>(this Range<T> left, Range<T> right)
            where T : IComparable<T>
        {
            return left.Intersection(right) != null;
        }

        public static Range<T> Intersection<T>(this Range<T> left, Range<T> right)
            where T : IComparable<T>
        {
            var start = left.start.Max(right.start);
            var end = left.end.Min(right.end);

            if (end.CompareTo(start) < 0)
            {
                return null;
            }

            return new Range<T> {start = start, end = end};
        } 

        public static Box<double> Bounding(this Circle<double> circle)
        {
            return new Box<double>
            {
                horizontal = Bounding(circle.radius, circle.center.x),
                vertical = Bounding(circle.radius, circle.center.y),
            };
        }

        private static Range<double> Bounding(double radius, double x)
        {
            return new Range<double>
            {
                start = x - radius,
                end = x + radius
            };
        }

        public static Vector<B> Fmap<A, B>(this Vector<A> vector, Func<A, B> select)
        {
            return new Vector<B>
            {
                x = select(vector.x),
                y = select(vector.y),
            };
        } 

        public static Circle<B> Fmap<A, B>(this Circle<A> circle, Func<A, B> select)
        {
            return new Circle<B>
            {
                radius = circle.radius,
                center = circle.center.Fmap(select),
            };
        }

        public static Point ToPoint(this Vector<int> vector)
        {
            return new Point(vector.x, vector.y);
        }

        public static PointF ToPoint(this Vector<double> vector)
        {
            return new PointF((float)vector.x, (float)vector.y);
        }

        public static Rectangle ToRect(this Box<int> box)
        {
            return new Rectangle(
                x: box.horizontal.start,
                y: box.vertical.start,
                width: box.horizontal.Size(), 
                height: box.vertical.Size());
        }

        public static RectangleF ToRect(this Box<double> box)
        {
            return new RectangleF(
                x: (float)box.horizontal.start,
                y: (float)box.vertical.start,
                width: (float)box.horizontal.Size(), 
                height: (float)box.vertical.Size());
        }

        public static Func<double, T, T> Continuously<T>(this Random random, Func<int, T, T> discrete)
        {
            return (amount, element) =>
            {
                var number = amount.Split();

                element = discrete(number.integral * number.sign, element);

                if (random.NextDouble() < number.fractional)
                {
                    element = discrete(number.sign, element);
                }

                return element;
            };
        }

        public static Number Split(this double number)
        {
            var magnitude = Math.Abs(number);

            var integral = (int) magnitude;

            return new Number
            {
                sign = Math.Sign(number),
                integral = integral,
                fractional = magnitude - integral,
            };
        }
    }
}