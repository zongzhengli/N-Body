﻿using System;
using System.Drawing;
using Lattice;

namespace NBody {

    /// <summary>
    /// The tree structure in the Barnes-Hut algorithm. 
    /// </summary>
    class Octree {

        /// <summary>
        /// Defines the accuracy-speed tradeoff value of the simulation. The acceleration of a Body by the Octree 
        /// is only calculated when the ratio of the width to distance (from the tree's center of mass to the Body) 
        /// ratio is less than this.
        /// </summary>
        private const Double Tolerance = .5;

        /// <summary>
        /// Defines the minimum width of an Octree. Subtrees are not created if their width would be smaller than 
        /// this value. 
        /// </summary>
        private const Double MinimumWidth = 1;

        /// <summary>
        /// The collection of subtrees for the tree. 
        /// </summary>
        private Octree[] Subtrees = null;

        /// <summary>
        /// The location of the center of the tree's bounds. 
        /// </summary>
        private Vector Location;

        /// <summary>
        /// The width of the tree's bounds. 
        /// </summary>
        private Double Width = 0;

        /// <summary>
        /// The location of the center of mass of the Bodies contained in the tree. 
        /// </summary>
        private Vector CenterOfMass = Vector.Zero;

        /// <summary>
        /// The total mass of the Bodies contained in the tree. 
        /// </summary>
        private Double TotalMass = 0;

        /// <summary>
        /// The number of Bodies in the tree. This is used to determine special cases when there are very few Bodies 
        /// in the tree. 
        /// </summary>
        private Int32 TotalBodies = 0;

        /// <summary>
        /// The first Body added to the tree. This is used when the first Body must be added to subtrees
        /// at a later time. 
        /// </summary>
        private Body FirstBody = null;

        /// <summary>
        /// Constructs an Octree with the given width located about the origin.
        /// </summary>
        /// <param name="width">The width of the new Octree.</param>
        public Octree(Double width) {
            Width = width;
        }

        /// <summary>
        /// Initializes an Octree with the given location and width.
        /// </summary>
        /// <param name="location">The location of the center of the new Octree.</param>
        /// <param name="width">The width of the new Octree.</param>
        public Octree(Vector location, Double width)
            : this(width) {
            Location = CenterOfMass = location;
        }

        /// <summary>
        /// Adds a Body to the tree and updates internal tree properties. If the tree contains more than one 
        /// Body afterwards, the Body is also added to the appropriate subtree. 
        /// </summary>
        /// <param name="body">The Body to add to the tree.</param>
        public void Add(Body body) {
            CenterOfMass = (TotalMass * CenterOfMass + body.Mass * body.Location) / (TotalMass + body.Mass);
            TotalMass += body.Mass;
            TotalBodies++;
            if (TotalBodies == 1)
                FirstBody = body;
            else
                AddToSubtree(body);
            if (TotalBodies == 2)
                AddToSubtree(FirstBody);
        }

        /// <summary>
        /// Adds a Body to the appropriate subtree based on its position in space. The subtree is initialized 
        /// if it has not already been done so. 
        /// </summary>
        /// <param name="body">The Body to add to a subtree.</param>
        private void AddToSubtree(Body body) {
            Double subtreeWidth = Width / 2;

            // Don't create subtrees if it violates the width limit.
            if (subtreeWidth < MinimumWidth)
                return;

            if (Subtrees == null)
                Subtrees = new Octree[8];

            // Determine which subtree the Body belongs in and add it to that subtree. 
            Int32 subtreeIndex = 0;
            for (Int32 i = -1; i <= 1; i += 2)
                for (Int32 j = -1; j <= 1; j += 2)
                    for (Int32 k = -1; k <= 1; k += 2) {
                        Vector subtreeLocation = new Vector(Location.X + i * subtreeWidth / 2,
                                                            Location.Y + j * subtreeWidth / 2,
                                                            Location.Z + k * subtreeWidth / 2);

                        // Determine if the body is contained within the bounds of the subtree under consideration. 
                        if (subtreeLocation.X - subtreeWidth / 2 < body.Location.X && body.Location.X <= subtreeLocation.X + subtreeWidth / 2
                            && subtreeLocation.Y - subtreeWidth / 2 < body.Location.Y && body.Location.Y <= subtreeLocation.Y + subtreeWidth / 2
                            && subtreeLocation.Z - subtreeWidth / 2 < body.Location.Z && body.Location.Z <= subtreeLocation.Z + subtreeWidth / 2) {

                            if (Subtrees[subtreeIndex] == null)
                                Subtrees[subtreeIndex] = new Octree(subtreeLocation, subtreeWidth);
                            Subtrees[subtreeIndex].Add(body);
                            return;
                        }
                        subtreeIndex++;
                    }
        }

        /// <summary>
        /// Updates the acceleration of a Body based on the properties of the tree. 
        /// </summary>
        /// <param name="body">The Body to accelerate.</param>
        public void Accelerate(Body body) {
            Double dx = CenterOfMass.X - body.Location.X;
            Double dy = CenterOfMass.Y - body.Location.Y;
            Double dz = CenterOfMass.Z - body.Location.Z;

            // Case 1. The tree contains only one Body and it isn't the Body to be accelerated. The second 
            //         condition (optimized for speed) determines if the Body to be accelerated lies outside 
            //         the bounds of the tree. If it does, it can't be the single Body so we perform the 
            //         acceleration. 
            if (TotalBodies == 1
                && ((body.Location.X - Location.X) * (body.Location.X - Location.X) * 4 > Width * Width
                   || (body.Location.Y - Location.Y) * (body.Location.Y - Location.Y) * 4 > Width * Width
                   || (body.Location.Z - Location.Z) * (body.Location.Z - Location.Z) * 4 > Width * Width))
                PerformAcceleration(body, dx, dy, dz);

            // Case 2. The width to distance ratio is within Tolerance, so we perform the acceleration. This  
            //         condition is an optimized equivalent of Width / (distance) < Tolerance. 
            else if (Width * Width < Tolerance * Tolerance * (dx * dx + dy * dy + dz * dz))
                PerformAcceleration(body, dx, dy, dz);

            // Case 3. We can't perform the acceleration, so we try to pass the Body on to the subtrees. 
            else
                if (Subtrees != null)
                    foreach (Octree subtree in Subtrees)
                        if (subtree != null)
                            subtree.Accelerate(body);
        }

        /// <summary>
        /// Calculates and applies the appropriate acceleration for a Body.
        /// </summary>
        /// <param name="body">The Body to accelerate.</param>
        /// <param name="dx">The difference between the tree's center of mass and the Body's position in the x axis.</param>
        /// <param name="dy">The difference between the tree's center of mass and the Body's position in the y axis.</param>
        /// <param name="dz">The difference between the tree's center of mass and the Body's position in the z axis.</param>
        private void PerformAcceleration(Body body, Double dx, Double dy, Double dz) {

            // Don't accelerate the Body if the center of mass is within its radius, to prevent crazy slingshot. 
            // This condition is an optimized equivalent of (distance) < body.Radius. 
            if (dx * dx + dy * dy + dz * dz < body.Radius * body.Radius)
                return;

            // Calculate a normalized acceleration value and multiply it with the displacement in each coordinate
            // to get that coordinate's acceleration componenet. 
            Double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            Double normAcc = World.G * TotalMass / (distance * distance * distance);

            body.Acceleration.X += normAcc * dx;
            body.Acceleration.Y += normAcc * dy;
            body.Acceleration.Z += normAcc * dz;
        }
    }
}