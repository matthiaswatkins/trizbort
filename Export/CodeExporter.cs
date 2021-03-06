/*
    Copyright (c) 2010-2015 by Genstein and Jason Lautzenheiser.

    This file is (or was originally) part of Trizbort, the Interactive Fiction Mapper.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace Trizbort.Export
{
  internal abstract class CodeExporter : IDisposable
  {
    /// <summary>
    ///   The collection of locations to export, indexed by their corresponding room.
    /// </summary>
    private readonly Dictionary<Room, Location> m_mapRoomToLocation = new Dictionary<Room, Location>();

    public CodeExporter()
    {
      LocationsInExportOrder = new List<Location>();
    }

    public abstract string FileDialogTitle { get; }
    public abstract List<KeyValuePair<string, string>> FileDialogFilters { get; }

    protected virtual Encoding Encoding
    {
      get { return Encoding.UTF8; }
    }

    protected abstract IEnumerable<string> ReservedWords { get; }

    protected static IEnumerable<AutomapDirection> AllDirections
    {
      get
      {
        foreach (AutomapDirection direction in Enum.GetValues(typeof (AutomapDirection)))
        {
          yield return direction;
        }
      }
    }

    /// <summary>
    ///   The collection of locations on the map, in the order in which they should be exported.
    /// </summary>
    protected List<Location> LocationsInExportOrder { get; private set; }

    public void Dispose()
    {
      Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    public string Export()
    {
      string ss;
      using (var writer = new StringWriter())
      {
        var title = Project.Current.Title;
        if (string.IsNullOrEmpty(title))
        {
          title = PathHelper.SafeGetFilenameWithoutExtension(Project.Current.FileName);
          if (string.IsNullOrEmpty(title))
          {
            title = "A Trizbort Map";
          }
        }
        var author = Project.Current.Author;
        if (string.IsNullOrEmpty(author))
        {
          author = "A Trizbort User";
        }

        ExportHeader(writer, title, author, Project.Current.Description ?? string.Empty);
        PrepareContent();
        ExportContent(writer);

        ss = writer.ToString();
      }
      return ss;
    }



    public void Export(string fileName)
    {
      using (var writer = Create(fileName))
      {
        var title = Project.Current.Title;
        if (string.IsNullOrEmpty(title))
        {
          title = PathHelper.SafeGetFilenameWithoutExtension(Project.Current.FileName);
          if (string.IsNullOrEmpty(title))
          {
            title = "A Trizbort Map";
          }
        }
        var author = Project.Current.Author;
        if (string.IsNullOrEmpty(author))
        {
          author = "A Trizbort User";
        }

        ExportHeader(writer, title, author, Project.Current.Description ?? string.Empty);
        PrepareContent();
        ExportContent(writer);
      }
    }

    protected virtual StreamWriter Create(string fileName)
    {
      return new StreamWriter(fileName, false, Encoding, 2 ^ 16);
    }

    protected abstract void ExportHeader(TextWriter writer, string title, string author, string description);
    protected abstract void ExportContent(TextWriter writer);
    protected abstract string GetExportName(Room room, int? suffix);
    protected abstract string GetExportNameForObject(string displayName, int? suffix);

    private void PrepareContent()
    {
      FindRooms();
      FindExits();
      PickBestExits();
      FindThings();
    }

    private void FindRooms()
    {
      var mapExportNameToRoom = new Dictionary<string, Room>(StringComparer.InvariantCultureIgnoreCase);

      // prevent use of reserved words
      foreach (var reservedWord in ReservedWords)
      {
        mapExportNameToRoom.Add(reservedWord, null);
      }

      foreach (var element in Project.Current.Elements)
      {
        if (element is Room)
        {
          var room = (Room) element;

          // assign each room a unique export name.
          var exportName = GetExportName(room, null);
          if (exportName == string.Empty)
            exportName = "object";
          var index = 2;
          while (mapExportNameToRoom.ContainsKey(exportName))
          {
            exportName = GetExportName(room, index++);
          }

          mapExportNameToRoom[exportName] = room;
          var location = new Location(room, exportName);
          LocationsInExportOrder.Add(location);
          m_mapRoomToLocation[room] = location;
        }
      }
    }

    private void FindExits()
    {
      // find the exits from each room,
      // file them by room, and assign them priorities.
      // don't decide yet which exit is "the" from a room in a particular direction,
      // since we need to compare all a room's exits for that.
      foreach (var element in Project.Current.Elements)
      {
        if (element is Connection)
        {
          var connection = (Connection) element;
          CompassPoint sourceCompassPoint, targetCompassPoint;
          var sourceRoom = connection.GetSourceRoom(out sourceCompassPoint);
          var targetRoom = connection.GetTargetRoom(out targetCompassPoint);

          if (sourceRoom == null || targetRoom == null)
          {
            // ignore fully or partially undocked connections
            continue;
          }

          if (sourceRoom == targetRoom && sourceCompassPoint == targetCompassPoint)
          {
            // ignore stub connections, such as from automapping
            continue;
          }

          Location sourceLocation, targetLocation;
          if (m_mapRoomToLocation.TryGetValue(sourceRoom, out sourceLocation) &&
              m_mapRoomToLocation.TryGetValue(targetRoom, out targetLocation))
          {
            sourceLocation.AddExit(new Exit(sourceLocation, targetLocation, sourceCompassPoint, connection.StartText, connection.Style));

            if (connection.Flow == ConnectionFlow.TwoWay)
            {
              targetLocation.AddExit(new Exit(targetLocation, sourceLocation, targetCompassPoint, connection.EndText, connection.Style));
            }
          }
        }
      }
    }

    private void FindThings()
    {
      var mapExportNameToThing = new Dictionary<string, Thing>(StringComparer.InvariantCultureIgnoreCase);

      // prevent use of reserved words
      foreach (var reservedWord in ReservedWords)
      {
        mapExportNameToThing.Add(reservedWord, null);
      }

      foreach (var rooms in LocationsInExportOrder)
      {
        mapExportNameToThing.Add(rooms.ExportName, null);
      }

      foreach (var location in LocationsInExportOrder)
      {
        var objectsText = location.Room.Objects;
        if (string.IsNullOrEmpty(objectsText))
        {
          continue;
        }

        var objectNames = objectsText.Replace("\r", string.Empty).Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
        foreach (var objectName in objectNames)
        {
          // the display name is simply the object name without indentation
          var displayName = objectName.Trim();

          if (string.IsNullOrEmpty(displayName))
          {
            continue;
          }

          // assign each thing a unique export name.
          var exportName = GetExportNameForObject(displayName, null);
          var index = 2;
          while (mapExportNameToThing.ContainsKey(exportName))
          {
            exportName = GetExportNameForObject(displayName, index++);
          }

          // on each line, indentation denotes containment;
          // work out how much indentation there is
          var indent = 0;
          while (indent < objectName.Length && objectName[indent] == ' ')
          {
            ++indent;
          }

          // compare indentations to deduce containment
          Thing container = null;
          for (var thingIndex = location.Things.Count - 1; thingIndex >= 0; --thingIndex)
          {
            var priorThing = location.Things[thingIndex];
            if (indent > priorThing.Indent)
            {
              container = priorThing;
              break;
            }
          }

          var thing = new Thing(displayName, exportName, location, container, indent);
          mapExportNameToThing.Add(exportName, thing);
          location.Things.Add(thing);
        }
      }
    }

    private void PickBestExits()
    {
      // for every direction from every room, if there are one or more exits
      // in said direction, pick the best one.
      foreach (var location in LocationsInExportOrder)
      {
        location.PickBestExits();
      }
    }

    protected class Location
    {
      private readonly List<Exit> m_exits = new List<Exit>();
      private readonly Dictionary<AutomapDirection, Exit> m_mapDirectionToBestExit = new Dictionary<AutomapDirection, Exit>();
      private readonly List<Thing> m_things = new List<Thing>();

      public Location(Room room, string exportName)
      {
        Room = room;
        ExportName = exportName;
      }

      public Room Room { get; private set; }
      public string ExportName { get; private set; }
      public bool Exported { get; set; }

      public List<Thing> Things
      {
        get { return m_things; }
      }

      public void AddExit(Exit exit)
      {
        m_exits.Add(exit);
      }

      public void PickBestExits()
      {
        m_mapDirectionToBestExit.Clear();
        foreach (var direction in AllDirections)
        {
          var exit = PickBestExit(direction);
          if (exit != null)
          {
            m_mapDirectionToBestExit.Add(direction, exit);
          }
        }
      }

      private Exit PickBestExit(AutomapDirection direction)
      {
        // sort exits by priority for this direction only
        m_exits.Sort((Exit a, Exit b) =>
        {
          var one = a.GetPriority(direction);
          var two = b.GetPriority(direction);
          return two - one;
        });

        // pick the highest priority exit if its direction matches;
        // if the highest priority exit's direction doesn't match,
        // there's no exit in this direction.
        if (m_exits.Count > 0)
        {
          var exit = m_exits[0];
          if (exit.PrimaryDirection == direction || exit.SecondaryDirection == direction)
          {
            return exit;
          }
        }
        return null;
      }

      public Exit GetBestExit(AutomapDirection direction)
      {
        Exit exit;
        if (m_mapDirectionToBestExit.TryGetValue(direction, out exit))
        {
          return exit;
        }
        return null;
      }
    }

    protected class Exit
    {
      /// <summary>
      ///   The priority of the this exit's primary direction, compared to other exits which may go in the same direction from
      ///   the same room.
      /// </summary>
      /// <remarks>
      ///   Since multiple exits may lead the same way from the same room, priorities are
      ///   used to decide which exit is the "best" exit in any direction.
      ///   For example, a northerly exit which is docked to the N compass point and which
      ///   does not go up, down, in or out is a higher priority than a northerly exit
      ///   docked to the NNE compass point and which also goes up.
      /// </remarks>
      private int m_primaryPriority;

      public Exit(Location source, Location target, CompassPoint visualCompassPoint, string connectionText, ConnectionStyle connectionStyle)
      {
        Source = source;
        Target = target;
        VisualCompassPoint = visualCompassPoint;
        Conditional = connectionStyle == ConnectionStyle.Dashed;

        AssignPrimaryPriority();
        AssignSecondaryDirection(connectionText);
        if (SecondaryDirection != null)
          PrimaryDirection = (AutomapDirection)SecondaryDirection;
        else
          AssignPrimaryDirection();
      }

      /// <summary>
      ///   The room from which this exit leads.
      /// </summary>
      public Location Source { get; private set; }

      /// <summary>
      ///   The room to which this exit leads.
      /// </summary>
      public Location Target { get; private set; }

      /// <summary>
      ///   The compass point in Trizbort at which this exit is docked to the starting room.
      /// </summary>
      /// <remarks>
      ///   Naturally this may include compass points such as SouthSouthWest need to be
      ///   translated into an exportable direction; see PrimaryDirection and SecondaryDirection.
      /// </remarks>
      public CompassPoint VisualCompassPoint { get; private set; }

      /// <summary>
      ///   The primary direction of this exit: N, S, E, W, NE, NW, SE, SW.
      ///   Deduced from VisualCompassPoint.
      /// </summary>
      public AutomapDirection PrimaryDirection { get; private set; }

      /// <summary>
      ///   The secondary direction of this exit, if any: either up, down, in or out.
      /// </summary>
      public AutomapDirection? SecondaryDirection { get; private set; }

      /// <summary>
      ///   True if this exit requires some in-game action from the player to be used; false otherwise.
      /// </summary>
      public bool Conditional { get; private set; }

      /// <summary>
      ///   True if this exit has been exported; false otherwise.
      /// </summary>
      public bool Exported { get; set; }

      /// <summary>
      ///   Get the priority of the exit, in the given direction, with respect to other exits.
      ///   Higher priorities indicate more suitable exits.
      /// </summary>
      public int GetPriority(AutomapDirection direction)
      {
        if (direction == PrimaryDirection)
        {
          return m_primaryPriority;
        }
        if (direction == SecondaryDirection)
        {
          return 1;
        }
        return -1;
      }

      private void AssignPrimaryDirection()
      {
        switch (VisualCompassPoint)
        {
          case CompassPoint.NorthNorthWest:
          case CompassPoint.North:
          case CompassPoint.NorthNorthEast:
            PrimaryDirection = AutomapDirection.North;
            break;
          case CompassPoint.NorthEast:
            PrimaryDirection = AutomapDirection.NorthEast;
            break;
          case CompassPoint.EastNorthEast:
          case CompassPoint.East:
          case CompassPoint.EastSouthEast:
            PrimaryDirection = AutomapDirection.East;
            break;
          case CompassPoint.SouthEast:
            PrimaryDirection = AutomapDirection.SouthEast;
            break;
          case CompassPoint.SouthSouthEast:
          case CompassPoint.South:
          case CompassPoint.SouthSouthWest:
            PrimaryDirection = AutomapDirection.South;
            break;
          case CompassPoint.SouthWest:
            PrimaryDirection = AutomapDirection.SouthWest;
            break;
          case CompassPoint.WestSouthWest:
          case CompassPoint.West:
          case CompassPoint.WestNorthWest:
            PrimaryDirection = AutomapDirection.West;
            break;
          case CompassPoint.NorthWest:
            PrimaryDirection = AutomapDirection.NorthWest;
            break;
          default:
            throw new InvalidOperationException("Unexpected compass point found on ");
        }
      }

      private void AssignSecondaryDirection(string connectionText)
      {
        switch (connectionText)
        {
          case Connection.Up:
            SecondaryDirection = AutomapDirection.Up;
            break;
          case Connection.Down:
            SecondaryDirection = AutomapDirection.Down;
            break;
          case Connection.In:
            SecondaryDirection = AutomapDirection.In;
            break;
          case Connection.Out:
            SecondaryDirection = AutomapDirection.Out;
            break;
          default:
            SecondaryDirection = null;
            break;
        }
      }

      private void AssignPrimaryPriority()
      {
        m_primaryPriority = 0;

        switch (VisualCompassPoint)
        {
          case CompassPoint.North:
          case CompassPoint.South:
          case CompassPoint.East:
          case CompassPoint.West:
          case CompassPoint.NorthEast:
          case CompassPoint.SouthEast:
          case CompassPoint.SouthWest:
          case CompassPoint.NorthWest:
            if (SecondaryDirection == null)
            {
              m_primaryPriority += 4;
            }
            else
            {
              m_primaryPriority -= 2;
            }
            break;
          default:
            if (SecondaryDirection == null)
            {
              m_primaryPriority += 3;
            }
            else
            {
              m_primaryPriority -= 1;
            }
            break;
        }
      }

      /// <summary>
      ///   Test whether an exit is reciprocated in the other direction; i.e. is there a bidirectional connection.
      /// </summary>
      public static bool IsReciprocated(Location source, AutomapDirection direction, Location target)
      {
        if (target != null)
        {
          var oppositeDirection = CompassPointHelper.GetOpposite(direction);
          var reciprocal = target.GetBestExit(oppositeDirection);
          if (reciprocal != null)
          {
            Debug.Assert(reciprocal.PrimaryDirection == oppositeDirection || reciprocal.SecondaryDirection == oppositeDirection, "Alleged opposite direction appears to lead somewhere else. Something went wrong whilst building the set of exits from each room.");
            return reciprocal.Target == source;
          }
        }
        return false;
      }
    }

    protected class Thing
    {
      public Thing(string displayName, string exportName, Location location, Thing container, int indent)
      {
        DisplayName = displayName;
        ExportName = exportName;
        Location = location;
        Container = container;
        Debug.Assert(container == null || container.Location == location, "Thing's container is not located in the same room as the thing.");
        if (container != null)
        {
          container.Contents.Add(this);
        }
        Indent = indent;
        Contents = new List<Thing>();
      }

      public string DisplayName { get; private set; }
      public string ExportName { get; private set; }
      public Location Location { get; private set; }
      public Thing Container { get; private set; }
      public int Indent { get; private set; }
      public List<Thing> Contents { get; private set; }
    }
  }
}