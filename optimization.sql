-- Optimization Indexes
-- Run this script to improve query performance

-- Indexes for Orders
-- Optimizes "WHERE OrderStatus IN (...)" queries used in Dashboard
CREATE INDEX idx_orders_status ON orders(OrderStatus);
-- Optimizes "ORDER BY OrderDate DESC, OrderTime DESC"
CREATE INDEX idx_orders_datetime ON orders(OrderDate, OrderTime);

-- Indexes for Reservations
-- Optimizes "WHERE DATE(EventDate) = CURDATE() AND ReservationStatus IN (...)"
CREATE INDEX idx_reservations_status_date ON reservations(ReservationStatus, EventDate);
-- Optimizes "ORDER BY EventTime ASC"
CREATE INDEX idx_reservations_time ON reservations(EventTime);

-- Indexes for Products
-- Optimizes "WHERE Category = ..." and joins
CREATE INDEX idx_products_category ON products(Category);

-- Indexes for Inventory Batches
-- Optimizes "WHERE IngredientID = ... AND BatchStatus = 'Active'"
CREATE INDEX idx_inventory_batches_status ON inventory_batches(IngredientID, BatchStatus);

-- Indexes for Product Ingredients
-- Optimizes joins in Inventory checks
CREATE INDEX idx_prod_ing_product ON product_ingredients(ProductID);
CREATE INDEX idx_prod_ing_ingredient ON product_ingredients(IngredientID);
