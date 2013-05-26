﻿using System;
using System.Drawing;
using Lattice;

namespace NBody {

    /// <summary>
    /// The massive body in the simulation. 
    /// </summary>
    class Body {

        /// <summary>
        /// Returns the radius defined for the given mass value. 
        /// </summary>
        /// <param name="mass">The mass to calculate a radius for.</param>
        /// <returns>The radius defined for the given mass value.</returns>
        public static Double GetRadius(Double mass) {

            // We assume all Bodies have the same density so volume is directly proportion to mass. We let 
            // mass = volume and use the inverse of the equation for the volume of a sphere given its radius 
            // to solve for the radius. The end result is arbitrarily scaled and added to a constant so the 
            // Body is generally visible for drawing. 
            return 10 * Math.Pow(3 * mass / (4 * Math.PI), 1 / 3.0) + 1;
        }

        /// <summary>
        /// The location of the Body. 
        /// </summary>
        public Vector Location = Vector.Zero;

        /// <summary>
        /// The velocity of the Body. 
        /// </summary>
        public Vector Velocity = Vector.Zero;

        /// <summary>
        /// The acceleration applied to the Body during a single simulation step. 
        /// </summary>
        public Vector Acceleration = Vector.Zero;

        /// <summary>
        /// The mass of the Body. 
        /// </summary>
        public Double Mass;

        /// <summary>
        /// The radius of the Body. 
        /// </summary>
        public Double Radius;

        /// <summary>
        /// Initializes a Body with the given mass. All other properties are assigned default values of zero. 
        /// </summary>
        /// <param name="mass">The mass of the new Body.</param>
        public Body(Double mass) {
            Mass = mass;
            Radius = GetRadius(mass);
        }

        /// <summary>
        /// Initializes a Body with the given location, mass, and velocity. Unspecified properties are assigned 
        /// default values of zero except for mass, which is given a value of 1e6.
        /// </summary>
        /// <param name="location">The location of the new Body.</param>
        /// <param name="mass">The mass of the new Body.</param>
        /// <param name="velocity">The velocity of the new Body.</param>
        public Body(Vector location, Double mass = 1e6, Vector velocity = new Vector())
            : this(mass) {
            Location = location;
            Velocity = velocity;
        }

        /// <summary>
        /// Updates the properties of the body. This method should be invoked at each time step. 
        /// </summary>
        public void Update() {
            Velocity += Acceleration;
            Double speed = Velocity.Magnitude();
            if (speed > World.C)
                Velocity *= World.C / speed;
            Location += Velocity;
            Acceleration = Vector.Zero;
        }

        /// <summary>
        /// Rotates the body along an arbitrary axis. 
        /// </summary>
        /// <param name="point">The starting point for the axis of rotation.</param>
        /// <param name="direction">The direction for the axis of rotation</param>
        /// <param name="angle">The angle to rotate by.</param>
        public void Rotate(Vector point, Vector direction, Double angle) {
            Location = Location.Rotate(point, direction, angle);

            // To rotate velocity and acceleration we have to adjust for the starting point for the axis of rotation. 
            // This way the vectors are effectively rotated about their own starting points. 
            Velocity += point;
            Velocity = Velocity.Rotate(point, direction, angle);
            Velocity -= point;
            Acceleration += point;
            Acceleration = Acceleration.Rotate(point, direction, angle);
            Acceleration -= point;
        }
    }
}