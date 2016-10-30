using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuantumInterpolation
{
    public sealed class Vector<T>
    {
        public T x;
        public T y;

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}", x, y);
        }
    }

    public class Coordinate
    {
        public double q;
        public double vq;
    }

    public sealed class Circle<T>
    {
        public double radius;
        public Vector<T> center;
    }

    public sealed class Box<T>
    {
        public Range<T> horizontal;
        public Range<T> vertical;
    }

    public sealed class Number
    {
        public int sign;
        public int integral;
        public double fractional;
    }

    public sealed class Range<T>
    {
        public T start;
        public T end;
    }
   
    public sealed class Game
    {
        public double dt;
        public double dx;
        
        public double gravity;
        public Vector<int> bounds;
        public Vector<int> vortex; 

        public Circle<Coordinate>[] balls;
    }

    public partial class Form1 : Form
    {
        public readonly Game Game;
        public readonly Random Random = new Random();
        public readonly Timer Timer = new Timer();

        public Form1()
        {
            InitializeComponent();
            
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint, true);

            var balls = new Circle<Coordinate>[20];

            for (int i = 0; i < balls.Length; i++)
            {
                balls[i] = new Circle<Coordinate>
                {
                    radius = 5 + i,
                    center = new Vector<Coordinate>
                    {
                        x = new Coordinate { q = (1 + i % 5) * 100, vq = 10, },
                        y = new Coordinate { q = (1 + i / 5) * 100, vq = i, },
                    },
                };
            }

            Game = new Game()
            {
                dt = 1/100f,
                dx = 1/10f,
                gravity = 0,
                vortex = null, 
                bounds = new Vector<int> { x = this.Width, y = this.Height, },
                balls = balls,
            };

            var before = DateTime.Now;
            Timer.Interval = (int) (this.Game.dt * 1000);
            Timer.Tick += (sender, args) =>
            {
                var now = DateTime.Now;

                Func<int, Game, Game> discrete = (dt, game) =>
                {
                    while (dt != 0)
                    {
                        dt--;

                        UpdateGame(game, this.ClientSize);
                    }

                    return game;
                };

                this.Random.Continuously<Game>(discrete)((now - before).TotalSeconds / this.Game.dt, this.Game);

                before = now;

                Invalidate();
            };
            
            Timer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            this.Game.vortex = new Vector<int> { x = e.X, y = e.Y, };
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            this.Game.vortex = null;
        }

        private static void UpdateGame(Game game, Size bounds)
        {
            game.bounds = new Vector<int> { x = bounds.Width, y = bounds.Height, };

            foreach (var ball in game.balls)
            {
                var before = ball.Fmap(coord => coord.q).Bounding();

                ball.center.x.q += ball.center.x.vq*game.dt/game.dx;
                ball.center.y.q += ball.center.y.vq*game.dt/game.dx;
                
                Collide(ball, before, 0, 0);
                Collide(ball, before, game.bounds.x, game.bounds.y);
            }
            
            for (int i = 0; i < game.balls.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    Collide(game.balls[i], game.balls[j]);
                }
            }
            
            foreach (var ball in game.balls)
            {
                ball.center.y.vq += game.gravity*game.dt;

                if (game.vortex != null)
                {
                    var qBall = ball.center.Fmap(coord => coord.q);

                    var radial = game.vortex.Fmap(_ => (double)_).Minus(qBall);

                    var vAcc = radial.Unit().Scale(1000/radial.Magnitude());

                    ball.center.x.vq += vAcc.x;
                    ball.center.y.vq += vAcc.y;
                }
            }
        }

        private static void Collide(Circle<Coordinate> ball, Box<double> before, int barrierX, int barrierY)
        {
            var after = ball.Fmap(coord => coord.q).Bounding();

            Collide(ball.center, before, after, barrierX, barrierY);
        }

        private static void Collide(Vector<Coordinate> coord, Box<double> before, Box<double> after, int barrierX, int barrierY)
        {
            Collide(coord.x, barrierX, before.horizontal, after.horizontal);
            Collide(coord.y, barrierY, before.vertical, after.vertical);
        }

        private static void Collide(Coordinate coord, int barrier, Range<double> before, Range<double> after)
        {
            var offset = Collision(barrier, before, after);

            coord.q += offset;
            coord.vq = (offset == 0) ? coord.vq : -coord.vq;
        }

        private static double Collision(int barrier, Range<double> before, Range<double> after)
        {
            var trajectory = before.Span(after);

            if (trajectory.Contains(barrier))
            {
                var direction = Math.Sign(after.end - before.end);

                return Collision(direction, barrier, after);
            }

            return 0;
        }

        private static double Collision(int direction, int barrier, Range<double> box)
        {
            var distance = box.Interpolate((direction + 1f)/2) - barrier;

            if (distance*direction > 0)
            {
                return -distance;
            }

            return 0;
        }

        private static void Collide(Circle<Coordinate> left, Circle<Coordinate> right)
        {
            var separation = left.center.Fmap(coord => coord.q).Minus(right.center.Fmap(coord => coord.q));

            var distance = (left.radius + right.radius);

            if (separation.Magnitude() < distance.Square())
            {
                var vLeft = left.center.Fmap(coord => coord.vq);
                var vRight = right.center.Fmap(coord => coord.vq);

                var fromLeft = vLeft.Along(separation.Unit());
                var fromRight = vRight.Along(separation.Unit());

                vLeft = vLeft.Minus(fromLeft).Plus(fromRight);
                vRight = vRight.Minus(fromRight).Plus(fromLeft);

                left.center.x.vq = vLeft.x;
                left.center.y.vq = vLeft.y;
                
                right.center.x.vq = vRight.x;
                right.center.y.vq = vRight.y;
            }
        }

        private static double Energy(Game game)
        {
            return Kinetic(game) + Potential(game);
        }
        
        private static double Kinetic(Game game)
        {
            double energy = 0;
            foreach (var ball in game.balls)
            {
                energy += ball.center.Fmap(coord => coord.vq).Magnitude() / 2;
            }

            return energy;
        }
        
        private static double Potential(Game game)
        {
            double energy = 0;
            foreach (var ball in game.balls)
            {
                energy += game.gravity * (game.bounds.y - ball.center.y.q) * game.dx;
            }

            return energy;
        }

        private static Vector<double> Momentum(Game game)
        {
            Vector<double> momemtum = new Vector<double>();
            foreach (var ball in game.balls)
            {
                momemtum.x += ball.center.x.vq;
                momemtum.y += ball.center.y.vq;
            }

            return momemtum;
        } 
        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                base.OnPaint(e);

                DrawGame(e.Graphics, this.Game);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while drawing: " + ex.Message);
            }
        }

        private static void DrawGame(Graphics graphics, Game game)
        {
            var area = new Rectangle(new Point(0, 0), new Size(game.bounds.x, game.bounds.y));

            graphics.FillRectangle(new SolidBrush(Color.White), area);

            graphics.DrawRectangle(new Pen(Color.Black, 2), area);

            foreach (var ball in game.balls)
            {
                var bounding = ball.Fmap(coord => coord.q).Bounding();

                graphics.DrawEllipse(new Pen(Color.Black), bounding.ToRect());
                graphics.FillEllipse(new SolidBrush(Color.Red), bounding.ToRect());

                //Write(graphics, "" + ball.center.Fmap(coord => coord.q), ball.Fmap(coord => coord.q).center);
            }
            
            Write(graphics, "" + Energy(game), new Vector<double> { x = 25, y = 10 });
            Write(graphics, Kinetic(game) + " + " + Potential(game), new Vector<double> { x = 25, y = 25 });
            Write(graphics, "" + Momentum(game), new Vector<double> { x = 25, y = 40 });
        }

        private static void Write(Graphics graphics, string text, Vector<double> location)
        {
            var font = new Font(FontFamily.GenericMonospace, 8);

            var brush = new SolidBrush(Color.Black);

            graphics.DrawString(text, font, brush, location.ToPoint());
        }
    }
}
