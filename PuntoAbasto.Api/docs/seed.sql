-- ════════════════════════════════════════════════════════════════
-- Punto Abasto — seed.sql
-- Ejecutar en el SQL Editor de Supabase DESPUÉS de schema.sql.
--
-- NOTA: los usuarios internos (admin/vendedor/delivery) se crean vía
-- Supabase Auth Dashboard o la Admin API (auth.admin.createUser), NO
-- con INSERT directo en auth.users. Por eso este seed no incluye
-- ningún usuario: los dos pedidos de ejemplo se mueven de estado sin
-- un usuario_id humano (queda NULL, ver comentario en schema.sql),
-- lo que además sirve para probar que los triggers de negocio
-- (descuento de stock, historial, totales de cliente) funcionan aun
-- cuando la transición no viene acompañada de un usuario.
-- ════════════════════════════════════════════════════════════════

-- ────────────────────────────────────────────────────────────────
-- CONFIG
-- ────────────────────────────────────────────────────────────────
INSERT INTO public.config (clave, valor, descripcion) VALUES
  ('whatsapp_numero', '59167834810', 'Número de WhatsApp para recibir pedidos'),
  ('nombre_negocio',  'Punto Abasto', 'Nombre comercial del negocio'),
  ('moneda',          'Bs', 'Código/símbolo de moneda usado en toda la UI'),
  ('frontend_url',    'https://puntoabasto.com.bo', 'URL pública del frontend'),
  ('pedido_prefijo',  'PED', 'Prefijo de numeración correlativa de pedidos'),
  ('nota_prefijo',    'NV', 'Prefijo de numeración correlativa de notas de venta');

-- ────────────────────────────────────────────────────────────────
-- CATEGORIAS
-- ────────────────────────────────────────────────────────────────
INSERT INTO public.categorias (id, nombre, slug, emoji, orden) VALUES
  (1, 'Frutas',   'frutas',   '🍎', 1),
  (2, 'Verduras', 'verduras', '🥦', 2);

SELECT setval(pg_get_serial_sequence('public.categorias', 'id'), 2, true);

-- ────────────────────────────────────────────────────────────────
-- PRODUCTOS + PRODUCTO_UNIDADES + INVENTARIO
-- Cada bloque usa CTEs para encadenar los ids generados sin tener
-- que copiar uuids a mano entre las tres tablas.
-- ────────────────────────────────────────────────────────────────

-- 1) Manzana Roja ---------------------------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (1, 'Manzana Roja', 'Manzana roja nacional, dulce y crocante.', '🍎',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/manzana-roja.jpg', NULL, 1)
  RETURNING id
), u AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 12.00, true, 1 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 60, 10, 150, 'kg' FROM u;

-- 2) Naranja Jugo -----------------------------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (1, 'Naranja Jugo', 'Naranja jugosa ideal para exprimir.', '🍊',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/naranja-jugo.jpg', NULL, 2)
  RETURNING id
), u AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 8.00, true, 1 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 70, 10, 180, 'kg' FROM u;

-- 3) Frutilla -----------------------------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (1, 'Frutilla', 'Frutilla fresca de temporada.', '🍓',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/frutilla.jpg', 'Temporada', 3)
  RETURNING id
), u AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 15.00, true, 1 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 30, 5, 100, 'kg' FROM u;

-- 4) Sandía (se vende por unidad, no por Kg) -----------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (1, 'Sandía', 'Sandía grande y dulce, ideal para compartir.', '🍉',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/sandia.jpg', NULL, 4)
  RETURNING id
), u AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Unidad', 25.00, true, 1 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 40, 5, 100, 'unidad' FROM u;

-- 5) Mango Tommy -----------------------------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (1, 'Mango Tommy', 'Mango Tommy importado, pulpa firme y dulce.', '🥭',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/mango-tommy.jpg', NULL, 5)
  RETURNING id
), u AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 14.00, true, 1 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 45, 8, 120, 'kg' FROM u;

-- 6) Banana -----------------------------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (1, 'Banana', 'Banana de exportación, punto óptimo de maduración.', '🍌',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/banana.jpg', 'Más vendido', 6)
  RETURNING id
), u AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 7.50, true, 1 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 90, 15, 200, 'kg' FROM u;

-- 7) Tomate (2 unidades: Kg y Caja) -----------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (2, 'Tomate', 'Tomate perita, ideal para salsas y ensaladas.', '🍅',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/tomate.jpg', NULL, 1)
  RETURNING id
), u_kg AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 5.00, true, 1 FROM p
  RETURNING id
), u_caja AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Caja', 55.00, false, 2 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 80, 10, 200, 'kg' FROM u_kg
UNION ALL
SELECT id, 5, 1, 20, 'caja' FROM u_caja;

-- 8) Cebolla (2 unidades: Kg y Arroba) --------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (2, 'Cebolla', 'Cebolla blanca cruceña.', '🧅',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/cebolla.jpg', NULL, 2)
  RETURNING id
), u_kg AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 6.00, true, 1 FROM p
  RETURNING id
), u_arroba AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Arroba', 65.00, false, 2 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 75, 10, 180, 'kg' FROM u_kg
UNION ALL
SELECT id, 4, 1, 15, 'arroba' FROM u_arroba;

-- 9) Papa Harinosa (2 unidades: Kg y Arroba) --------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (2, 'Papa Harinosa', 'Papa harinosa, ideal para puré y fritura.', '🥔',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/papa-harinosa.jpg', NULL, 3)
  RETURNING id
), u_kg AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 4.50, true, 1 FROM p
  RETURNING id
), u_arroba AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Arroba', 45.00, false, 2 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 100, 15, 250, 'kg' FROM u_kg
UNION ALL
SELECT id, 6, 1, 20, 'arroba' FROM u_arroba;

-- 10) Zanahoria -----------------------------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (2, 'Zanahoria', 'Zanahoria fresca, rica en fibra.', '🥕',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/zanahoria.jpg', NULL, 4)
  RETURNING id
), u AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 5.50, true, 1 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 65, 10, 150, 'kg' FROM u;

-- 11) Ají Cambita -----------------------------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (2, 'Ají Cambita', 'Ají cambita picante, infaltable en la cocina cruceña.', '🌶️',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/aji-cambita.jpg', NULL, 5)
  RETURNING id
), u AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 20.00, true, 1 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 15, 3, 50, 'kg' FROM u;

-- 12) Brócoli -----------------------------------------------------
WITH p AS (
  INSERT INTO public.productos (categoria_id, nombre, descripcion, emoji, imagen_url, badge, orden)
  VALUES (2, 'Brócoli', 'Brócoli fresco, cosechado bajo pedido.', '🥦',
          'https://xxxx.supabase.co/storage/v1/object/public/productos/brocoli.jpg', 'Nuevo', 6)
  RETURNING id
), u AS (
  INSERT INTO public.producto_unidades (producto_id, label, precio, es_default, orden)
  SELECT id, 'Kg', 9.00, true, 1 FROM p
  RETURNING id
)
INSERT INTO public.inventario (producto_unidad_id, stock_actual, stock_minimo, stock_maximo, unidad_medida)
SELECT id, 35, 5, 100, 'kg' FROM u;

-- ────────────────────────────────────────────────────────────────
-- CLIENTES de prueba
-- ────────────────────────────────────────────────────────────────
INSERT INTO public.clientes (nombre, telefono, direccion, referencia_direccion, email) VALUES
  ('María Flores',  '76543210', 'Av. Banzer km 5, Urb. Los Pinos',      NULL, NULL),
  ('Carlos Méndez', '71234567', 'Barrio Hamacas, calle 3 #42',          NULL, NULL),
  ('Ana López',      '79876543', 'Urb. Villa Olímpica, mz 12 lt 8',      NULL, NULL);

-- ────────────────────────────────────────────────────────────────
-- PEDIDO 1 — María Flores — 2 Kg Tomate + 1 Kg Frutilla — entregado
-- ────────────────────────────────────────────────────────────────
WITH cliente AS (
  SELECT id FROM public.clientes WHERE telefono = '76543210'
), pedido AS (
  INSERT INTO public.pedidos (numero, cliente_id, estado, origen, subtotal, descuento, total, fecha_entrega_est)
  SELECT 'PED-0001', cliente.id, 'recibido', 'whatsapp', 25.00, 0, 25.00, now() + interval '24 hours'
  FROM cliente
  RETURNING id
), tomate_kg AS (
  SELECT pu.id, pu.precio
  FROM public.producto_unidades pu
  JOIN public.productos p ON p.id = pu.producto_id
  WHERE p.nombre = 'Tomate' AND pu.label = 'Kg'
), frutilla_kg AS (
  SELECT pu.id, pu.precio
  FROM public.producto_unidades pu
  JOIN public.productos p ON p.id = pu.producto_id
  WHERE p.nombre = 'Frutilla' AND pu.label = 'Kg'
)
INSERT INTO public.pedido_items (pedido_id, producto_unidad_id, producto_nombre, unidad_label, precio_unit, cantidad, subtotal)
SELECT pedido.id, tomate_kg.id, 'Tomate', 'Kg', tomate_kg.precio, 2, tomate_kg.precio * 2 FROM pedido, tomate_kg
UNION ALL
SELECT pedido.id, frutilla_kg.id, 'Frutilla', 'Kg', frutilla_kg.precio, 1, frutilla_kg.precio * 1 FROM pedido, frutilla_kg;

-- Recorre el flujo real de estados para que los triggers de negocio
-- (descuento de stock al confirmar, historial, totales del cliente al
-- entregar) se ejerciten igual que en producción.
UPDATE public.pedidos SET estado = 'confirmado' WHERE numero = 'PED-0001';
UPDATE public.pedidos SET estado = 'preparando' WHERE numero = 'PED-0001';
UPDATE public.pedidos SET estado = 'en_camino'  WHERE numero = 'PED-0001';
UPDATE public.pedidos SET estado = 'entregado', fecha_entrega_real = now() WHERE numero = 'PED-0001';

INSERT INTO public.notas_venta (numero, pedido_id, subtotal, descuento, total, estado)
SELECT 'NV-0001', id, subtotal, descuento, total, 'emitida'
FROM public.pedidos WHERE numero = 'PED-0001';

-- ────────────────────────────────────────────────────────────────
-- PEDIDO 2 — Carlos Méndez — 3 Kg Cebolla + 1 Amarro Cilantro — preparando
--
-- El cilantro no forma parte del catálogo sembrado (solo hay 6 frutas
-- y 6 verduras). PEDIDO_ITEMS está diseñado justamente para esto:
-- producto_unidad_id es nullable y el nombre/unidad/precio quedan
-- como snapshot inmutable, así que el item se registra igual aunque
-- no exista (o ya no exista) como producto en el catálogo/inventario.
-- ────────────────────────────────────────────────────────────────
WITH cliente AS (
  SELECT id FROM public.clientes WHERE telefono = '71234567'
), pedido AS (
  INSERT INTO public.pedidos (numero, cliente_id, estado, origen, subtotal, descuento, total, fecha_entrega_est)
  SELECT 'PED-0002', cliente.id, 'recibido', 'whatsapp', 21.00, 0, 21.00, now() + interval '24 hours'
  FROM cliente
  RETURNING id
), cebolla_kg AS (
  SELECT pu.id, pu.precio
  FROM public.producto_unidades pu
  JOIN public.productos p ON p.id = pu.producto_id
  WHERE p.nombre = 'Cebolla' AND pu.label = 'Kg'
)
INSERT INTO public.pedido_items (pedido_id, producto_unidad_id, producto_nombre, unidad_label, precio_unit, cantidad, subtotal)
SELECT pedido.id, cebolla_kg.id, 'Cebolla', 'Kg', cebolla_kg.precio, 3, cebolla_kg.precio * 3 FROM pedido, cebolla_kg
UNION ALL
SELECT pedido.id, NULL, 'Cilantro', 'Amarro', 3.00, 1, 3.00 FROM pedido;

UPDATE public.pedidos SET estado = 'confirmado' WHERE numero = 'PED-0002';
UPDATE public.pedidos SET estado = 'preparando' WHERE numero = 'PED-0002';

-- ────────────────────────────────────────────────────────────────
-- Sincronizar las sequences de numeración con lo que este seed acaba
-- de insertar a mano (PED-0001, PED-0002, NV-0001). Sin esto, el
-- primer pedido creado por la API generaría "PED-0001" de nuevo y
-- chocaría con la unique constraint de PEDIDOS.numero.
-- ────────────────────────────────────────────────────────────────
SELECT setval('public.pedido_numero_seq', 2, true);
SELECT setval('public.nota_numero_seq', 1, true);
