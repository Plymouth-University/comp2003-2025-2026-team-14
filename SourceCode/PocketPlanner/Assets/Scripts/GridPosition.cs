using UnityEngine; // Needed for Serializable attribute

[System.Serializable]
public struct GridPosition 
{
    public int x;
    public int y;

    public GridPosition(int y, int x) 
    {
        this.y = y;
        this.x = x;
    }

    public static bool operator ==(GridPosition a, GridPosition b) 
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(GridPosition a, GridPosition b) 
    {
        return !(a == b);
    }

    public static bool operator <(GridPosition a, GridPosition b) 
    {
        if (a.y < b.y) return true;
        if (a.y == b.y && a.x < b.x) return true;
        return false;
    }

    public static bool operator >(GridPosition a, GridPosition b) 
    {
        if (a.y > b.y) return true;
        if (a.y == b.y && a.x > b.x) return true;
        return false;
    }
    
    // Override Equals() and GetHashCode() is required in C# when overriding ==
    public override bool Equals(object obj) => obj is GridPosition other && this == other;
    public override int GetHashCode() => (x, y).GetHashCode();
}