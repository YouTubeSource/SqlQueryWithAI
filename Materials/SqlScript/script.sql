-- ================================================
-- Natural Language Query System - Database Setup
-- ================================================

-- Drop table if exists (for clean setup)
IF OBJECT_ID('Orders', 'U') IS NOT NULL 
    DROP TABLE Orders;
GO

-- Create Orders table
CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    CustomerName NVARCHAR(100) NOT NULL,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    ShippingCity NVARCHAR(100) NULL,
    ProductCategory NVARCHAR(50) NULL
);
GO

-- Insert sample data with variety for testing
-- Recent orders (last 7 days)
INSERT INTO Orders (CustomerName, OrderDate, TotalAmount, Status, ShippingCity, ProductCategory) VALUES
('John Smith', DATEADD(day, -1, GETDATE()), 150.00, 'Completed', 'New York', 'Electronics'),
('Jane Doe', DATEADD(day, -2, GETDATE()), 275.50, 'Completed', 'Los Angeles', 'Clothing'),
('Alice Williams', DATEADD(day, -3, GETDATE()), 320.00, 'Completed', 'Chicago', 'Books'),
('George Martin', DATEADD(day, -4, GETDATE()), 540.00, 'Pending', 'Houston', 'Electronics'),
('Helen Hunt', DATEADD(day, -5, GETDATE()), 88.88, 'Shipped', 'Phoenix', 'Home & Garden'),
('Bob Johnson', DATEADD(day, -6, GETDATE()), 89.99, 'Pending', 'Philadelphia', 'Sports'),
('Diana Prince', DATEADD(day, -7, GETDATE()), 125.00, 'Shipped', 'San Antonio', 'Clothing');

-- Older orders (8-14 days ago)
INSERT INTO Orders (CustomerName, OrderDate, TotalAmount, Status, ShippingCity, ProductCategory) VALUES
('Charlie Brown', DATEADD(day, -8, GETDATE()), 450.75, 'Completed', 'San Diego', 'Electronics'),
('Edward Norton', DATEADD(day, -9, GETDATE()), 199.99, 'Completed', 'Dallas', 'Books'),
('Kevin Spacey', DATEADD(day, -10, GETDATE()), 175.25, 'Completed', 'San Jose', 'Sports'),
('Laura Linney', DATEADD(day, -12, GETDATE()), 95.00, 'Completed', 'Austin', 'Home & Garden'),
('Michael Jordan', DATEADD(day, -14, GETDATE()), 680.00, 'Completed', 'Jacksonville', 'Electronics');

-- Much older orders (15-30 days ago)
INSERT INTO Orders (CustomerName, OrderDate, TotalAmount, Status, ShippingCity, ProductCategory) VALUES
('Fiona Apple', DATEADD(day, -15, GETDATE()), 75.50, 'Completed', 'Fort Worth', 'Clothing'),
('Ian McKellen', DATEADD(day, -18, GETDATE()), 399.99, 'Completed', 'Columbus', 'Electronics'),
('Julia Roberts', DATEADD(day, -20, GETDATE()), 225.00, 'Completed', 'Charlotte', 'Books'),
('Robert Downey', DATEADD(day, -22, GETDATE()), 510.00, 'Completed', 'San Francisco', 'Electronics'),
('Emma Watson', DATEADD(day, -25, GETDATE()), 145.75, 'Completed', 'Indianapolis', 'Clothing'),
('Tom Hanks', DATEADD(day, -28, GETDATE()), 299.99, 'Completed', 'Seattle', 'Sports'),
('Meryl Streep', DATEADD(day, -30, GETDATE()), 420.50, 'Completed', 'Denver', 'Home & Garden');

-- Very old orders (more than 30 days)
INSERT INTO Orders (CustomerName, OrderDate, TotalAmount, Status, ShippingCity, ProductCategory) VALUES
('Brad Pitt', DATEADD(day, -35, GETDATE()), 189.99, 'Completed', 'Boston', 'Electronics'),
('Angelina Jolie', DATEADD(day, -40, GETDATE()), 375.00, 'Completed', 'Nashville', 'Clothing'),
('Leonardo DiCaprio', DATEADD(day, -45, GETDATE()), 599.99, 'Completed', 'Detroit', 'Electronics'),
('Sandra Bullock', DATEADD(day, -50, GETDATE()), 125.50, 'Completed', 'Portland', 'Books'),
('Matt Damon', DATEADD(day, -60, GETDATE()), 450.00, 'Completed', 'Las Vegas', 'Sports'),
('Jennifer Lawrence', DATEADD(day, -75, GETDATE()), 275.99, 'Completed', 'Memphis', 'Clothing');
GO

-- ================================================
-- Verification Queries
-- ================================================

-- Total orders count
SELECT COUNT(*) AS TotalOrders FROM Orders;

-- Orders in last 7 days
SELECT COUNT(*) AS OrdersLast7Days 
FROM Orders 
WHERE OrderDate >= DATEADD(day, -7, GETDATE());

-- Total sales
SELECT SUM(TotalAmount) AS TotalSales FROM Orders;

-- Orders by status
SELECT Status, COUNT(*) AS Count, SUM(TotalAmount) AS TotalAmount
FROM Orders 
GROUP BY Status
ORDER BY Count DESC;

-- Orders by category
SELECT ProductCategory, COUNT(*) AS Count, SUM(TotalAmount) AS TotalAmount
FROM Orders 
GROUP BY ProductCategory
ORDER BY TotalAmount DESC;

-- Recent orders (last 7 days) detailed
SELECT OrderID, CustomerName, OrderDate, TotalAmount, Status, ProductCategory
FROM Orders
WHERE OrderDate >= DATEADD(day, -7, GETDATE())
ORDER BY OrderDate DESC;

-- Average order value
SELECT AVG(TotalAmount) AS AverageOrderValue FROM Orders;

-- Top 5 customers by spending
SELECT TOP 5 CustomerName, SUM(TotalAmount) AS TotalSpent, COUNT(*) AS OrderCount
FROM Orders
GROUP BY CustomerName
ORDER BY TotalSpent DESC;

-- Orders by city
SELECT ShippingCity, COUNT(*) AS OrderCount, SUM(TotalAmount) AS TotalAmount
FROM Orders
GROUP BY ShippingCity
ORDER BY TotalAmount DESC;

PRINT 'Database setup completed successfully!';
PRINT 'Total records inserted: 25';
GO