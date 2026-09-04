-- ════════════════════════════════════════════════════════════════
-- Punto Abasto — schema.sql
-- Ejecutar completo en el SQL Editor de Supabase (proyecto Pro).
--
-- Notas para Supabase:
--   - gen_random_uuid() ya está disponible por defecto (pgcrypto/pgcore),
--     no hace falta CREATE EXTENSION uuid-ossp.
--   - Todo vive en el schema "public" (default de Supabase).
--   - No se crea ninguna tabla de autenticación: auth.users la maneja
--     Supabase Auth. La tabla public.usuarios es un espejo liviano que
--     se llena vía trigger cuando se crea un usuario en auth.users.
--   - No existe REFRESH_TOKENS: Supabase Auth ya maneja sus propios
--     refresh tokens internamente.
-- ════════════════════════════════════════════════════════════════

-- ────────────────────────────────────────────────────────────────
-- 1. USUARIOS (espejo de auth.users, solo personal interno)
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.usuarios (
    id            uuid PRIMARY KEY REFERENCES auth.users (id) ON DELETE CASCADE,
    nombre        varchar(100) NOT NULL,
    email         varchar(150) NOT NULL UNIQUE,
    rol           varchar(20)  NOT NULL DEFAULT 'vendedor'
                  CONSTRAINT ck_usuarios_rol CHECK (rol IN ('admin', 'vendedor', 'delivery', 'almacenero')),
    activo        boolean NOT NULL DEFAULT true,
    ultimo_login  timestamptz,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_usuarios_rol ON public.usuarios (rol);
CREATE INDEX ix_usuarios_activo ON public.usuarios (activo);

-- Trigger: crea la fila espejo en public.usuarios cuando Supabase Auth
-- crea un usuario nuevo. El rol y el nombre se leen de user_metadata,
-- que el panel de administración (o el Auth Dashboard) debe setear al
-- invitar/crear al usuario interno.
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger AS $$
BEGIN
  INSERT INTO public.usuarios (id, email, nombre, rol)
  VALUES (
    new.id,
    new.email,
    COALESCE(new.raw_user_meta_data->>'nombre', split_part(new.email, '@', 1)),
    COALESCE(new.raw_user_meta_data->>'rol', 'vendedor')
  )
  ON CONFLICT (id) DO NOTHING;
  RETURN new;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER SET search_path = public;

CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- Mantiene public.usuarios sincronizado si cambia el rol/nombre/email
-- desde el Auth Dashboard (user_metadata) o el propio auth.users.email.
CREATE OR REPLACE FUNCTION public.handle_updated_user()
RETURNS trigger AS $$
BEGIN
  UPDATE public.usuarios
  SET email = new.email,
      nombre = COALESCE(new.raw_user_meta_data->>'nombre', public.usuarios.nombre),
      rol = COALESCE(new.raw_user_meta_data->>'rol', public.usuarios.rol),
      updated_at = now()
  WHERE id = new.id;
  RETURN new;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER SET search_path = public;

CREATE TRIGGER on_auth_user_updated
  AFTER UPDATE ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_updated_user();

-- ────────────────────────────────────────────────────────────────
-- 2. CLIENTES
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.clientes (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre                varchar(100) NOT NULL,
    telefono              varchar(20) NOT NULL UNIQUE,
    direccion             text NOT NULL,
    referencia_direccion  text,
    -- Link de Google Maps o "lat,long" que el negocio pide por WhatsApp cuando
    -- la dirección de texto no alcanza para ubicar la entrega.
    ubicacion_gps         text,
    email                 varchar(150),
    total_pedidos         int NOT NULL DEFAULT 0 CHECK (total_pedidos >= 0),
    total_gastado         decimal(10, 2) NOT NULL DEFAULT 0 CHECK (total_gastado >= 0),
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_clientes_telefono ON public.clientes (telefono);

-- ────────────────────────────────────────────────────────────────
-- 3. CATEGORIAS
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.categorias (
    id      serial PRIMARY KEY,
    nombre  varchar(80) NOT NULL UNIQUE,
    slug    varchar(80) NOT NULL UNIQUE,
    emoji   varchar(10),
    orden   int NOT NULL DEFAULT 0
);

-- ────────────────────────────────────────────────────────────────
-- 4. PRODUCTOS
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.productos (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    categoria_id  int NOT NULL REFERENCES public.categorias (id) ON DELETE RESTRICT,
    nombre        varchar(150) NOT NULL,
    descripcion   text,
    emoji         varchar(10),
    imagen_url    varchar(255),
    badge         varchar(50),
    activo        boolean NOT NULL DEFAULT true,
    disponible    boolean NOT NULL DEFAULT true,
    orden         int NOT NULL DEFAULT 0,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_productos_categoria_id ON public.productos (categoria_id);
CREATE INDEX ix_productos_activo_disponible ON public.productos (activo, disponible);

-- ────────────────────────────────────────────────────────────────
-- 5. PRODUCTO_UNIDADES
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.producto_unidades (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    producto_id   uuid NOT NULL REFERENCES public.productos (id) ON DELETE CASCADE,
    label         varchar(50) NOT NULL,
    precio        decimal(10, 2) NOT NULL CHECK (precio >= 0),
    es_default    boolean NOT NULL DEFAULT false,
    disponible    boolean NOT NULL DEFAULT true,
    orden         int NOT NULL DEFAULT 0
);

CREATE INDEX ix_producto_unidades_producto_id ON public.producto_unidades (producto_id);
-- Una sola unidad "default" por producto
CREATE UNIQUE INDEX ux_producto_unidades_default
    ON public.producto_unidades (producto_id)
    WHERE es_default = true;

-- ────────────────────────────────────────────────────────────────
-- 6. PEDIDOS
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.pedidos (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    numero              varchar(20) NOT NULL UNIQUE,
    cliente_id          uuid NOT NULL REFERENCES public.clientes (id) ON DELETE RESTRICT,
    usuario_id          uuid REFERENCES public.usuarios (id) ON DELETE SET NULL,
    estado              varchar(20) NOT NULL DEFAULT 'recibido'
                        CONSTRAINT ck_pedidos_estado CHECK (
                          estado IN ('recibido', 'confirmado', 'preparando', 'en_camino', 'entregado', 'cancelado')
                        ),
    origen              varchar(20) NOT NULL DEFAULT 'whatsapp'
                        CONSTRAINT ck_pedidos_origen CHECK (origen IN ('whatsapp', 'web', 'telefono')),
    subtotal            decimal(10, 2) NOT NULL CHECK (subtotal >= 0),
    descuento           decimal(10, 2) NOT NULL DEFAULT 0 CHECK (descuento >= 0),
    total               decimal(10, 2) NOT NULL CHECK (total >= 0),
    -- Independiente de "estado": la entrega y el pago son hechos distintos
    -- (se puede entregar y cobrar días después). No participa de la máquina
    -- de estados en PedidoEstadoTransiciones.
    pagado              boolean NOT NULL DEFAULT false,
    -- Null si pagado es false; requerido por la API al marcar pagado = true.
    metodo_pago         varchar(20)
                        CONSTRAINT ck_pedidos_metodo_pago CHECK (metodo_pago IN ('qr', 'efectivo', 'transferencia')),
    fecha_pago          timestamptz,
    -- Si es true, total incluye 16% de IVA sobre (subtotal - descuento).
    facturado           boolean NOT NULL DEFAULT false,
    notas               text,
    fecha_pedido        timestamptz NOT NULL DEFAULT now(),
    fecha_entrega_est   timestamptz,
    fecha_entrega_real  timestamptz,
    created_at          timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_pedidos_cliente_id ON public.pedidos (cliente_id);
CREATE INDEX ix_pedidos_usuario_id ON public.pedidos (usuario_id);
CREATE INDEX ix_pedidos_estado ON public.pedidos (estado);
CREATE INDEX ix_pedidos_fecha_pedido ON public.pedidos (fecha_pedido DESC);
CREATE INDEX ix_pedidos_pagado ON public.pedidos (pagado);

-- ────────────────────────────────────────────────────────────────
-- 7. PEDIDO_ITEMS (snapshot inmutable de nombre/unidad/precio)
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.pedido_items (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id           uuid NOT NULL REFERENCES public.pedidos (id) ON DELETE CASCADE,
    producto_unidad_id  uuid REFERENCES public.producto_unidades (id) ON DELETE SET NULL,
    producto_nombre     varchar(150) NOT NULL,
    unidad_label        varchar(50) NOT NULL,
    precio_unit         decimal(10, 2) NOT NULL CHECK (precio_unit >= 0),
    cantidad            decimal(10, 3) NOT NULL CHECK (cantidad > 0),
    subtotal            decimal(10, 2) NOT NULL CHECK (subtotal >= 0)
);

CREATE INDEX ix_pedido_items_pedido_id ON public.pedido_items (pedido_id);
CREATE INDEX ix_pedido_items_producto_unidad_id ON public.pedido_items (producto_unidad_id);

-- ────────────────────────────────────────────────────────────────
-- 8. PEDIDO_ITEM_PRECIO_HISTORIAL (quién corrigió un precio diferenciado)
--    Snapshot inmutable, igual que PEDIDO_ESTADOS: no se actualiza ni se
--    borra, solo se inserta una fila por cada corrección de precio_unit
--    en PEDIDO_ITEMS (ver PATCH /api/pedidos/{id}/items/{itemId}/precio).
--    usuario_id es nullable por la misma razón que en PEDIDO_ESTADOS.
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.pedido_item_precio_historial (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_item_id   uuid NOT NULL REFERENCES public.pedido_items (id) ON DELETE CASCADE,
    usuario_id       uuid REFERENCES public.usuarios (id) ON DELETE SET NULL,
    precio_anterior  decimal(10, 2) NOT NULL CHECK (precio_anterior >= 0),
    precio_nuevo     decimal(10, 2) NOT NULL CHECK (precio_nuevo >= 0),
    created_at       timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_pedido_item_precio_historial_pedido_item_id ON public.pedido_item_precio_historial (pedido_item_id);

-- ────────────────────────────────────────────────────────────────
-- 9. PEDIDO_ESTADOS (historial de transiciones)
--    usuario_id es nullable: los triggers de negocio (confirmación,
--    cancelación) pueden correr sin un usuario humano detrás (seed
--    data, jobs internos), y se registran como acción del sistema.
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.pedido_estados (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id        uuid NOT NULL REFERENCES public.pedidos (id) ON DELETE CASCADE,
    usuario_id       uuid REFERENCES public.usuarios (id) ON DELETE SET NULL,
    estado_anterior  varchar(20),
    estado_nuevo     varchar(20) NOT NULL,
    observacion      text,
    created_at       timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_pedido_estados_pedido_id ON public.pedido_estados (pedido_id);

-- ────────────────────────────────────────────────────────────────
-- 10. PEDIDO_PAGO_HISTORIAL (quién marcó/desmarcó el pago)
--     Snapshot inmutable, mismo patrón que PEDIDO_ESTADOS: una fila por
--     cada toggle de PEDIDOS.pagado (ver PATCH /api/pedidos/{id}/pago).
--     usuario_id es nullable por la misma razón que en PEDIDO_ESTADOS.
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.pedido_pago_historial (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id   uuid NOT NULL REFERENCES public.pedidos (id) ON DELETE CASCADE,
    usuario_id  uuid REFERENCES public.usuarios (id) ON DELETE SET NULL,
    pagado      boolean NOT NULL,
    metodo_pago varchar(20)
                CONSTRAINT ck_pedido_pago_historial_metodo_pago CHECK (metodo_pago IN ('qr', 'efectivo', 'transferencia')),
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_pedido_pago_historial_pedido_id ON public.pedido_pago_historial (pedido_id);

-- ────────────────────────────────────────────────────────────────
-- 11. INVENTARIO (por PRODUCTO_UNIDADES, no por PRODUCTOS)
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.inventario (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    producto_unidad_id  uuid NOT NULL UNIQUE REFERENCES public.producto_unidades (id) ON DELETE CASCADE,
    stock_actual        decimal(10, 3) NOT NULL DEFAULT 0 CHECK (stock_actual >= 0),
    stock_minimo        decimal(10, 3) NOT NULL DEFAULT 0 CHECK (stock_minimo >= 0),
    stock_maximo        decimal(10, 3) CHECK (stock_maximo IS NULL OR stock_maximo >= stock_minimo),
    unidad_medida       varchar(30) NOT NULL,
    alerta_activa       boolean NOT NULL DEFAULT false,
    updated_at          timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_inventario_alerta_activa ON public.inventario (alerta_activa) WHERE alerta_activa = true;

-- ────────────────────────────────────────────────────────────────
-- 11.1 COMPRAS
--     Registro de compras a proveedor, para el costeo de utilidades
--     (cuánto se gasta comprando vs. cuánto se vende — ver reporte
--     /api/reportes/costeo). Cada compra genera además un movimiento
--     "entrada" en INVENTARIO_MOVIMIENTOS (compra_id abajo) para que
--     el stock quede consistente, mismo patrón que un pedido genera
--     una "salida". usuario_id nullable por la misma razón que en
--     PEDIDO_ESTADOS.
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.compras (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    producto_unidad_id  uuid NOT NULL REFERENCES public.producto_unidades (id) ON DELETE RESTRICT,
    usuario_id          uuid REFERENCES public.usuarios (id) ON DELETE SET NULL,
    cantidad            decimal(10, 3) NOT NULL CHECK (cantidad > 0),
    costo_unitario      decimal(10, 2) NOT NULL CHECK (costo_unitario > 0),
    costo_total         decimal(10, 2) NOT NULL CHECK (costo_total > 0),
    proveedor           varchar(150),
    notas               text,
    created_at          timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_compras_producto_unidad_id ON public.compras (producto_unidad_id);
CREATE INDEX ix_compras_created_at ON public.compras (created_at DESC);

-- ────────────────────────────────────────────────────────────────
-- 12. INVENTARIO_MOVIMIENTOS
--     usuario_id es nullable por la misma razón que en PEDIDO_ESTADOS:
--     un movimiento disparado automáticamente por un trigger (stock
--     descontado al confirmar, revertido al cancelar) no siempre tiene
--     un usuario humano detrás.
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.inventario_movimientos (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    inventario_id    uuid NOT NULL REFERENCES public.inventario (id) ON DELETE CASCADE,
    pedido_id        uuid REFERENCES public.pedidos (id) ON DELETE SET NULL,
    compra_id        uuid REFERENCES public.compras (id) ON DELETE SET NULL,
    usuario_id       uuid REFERENCES public.usuarios (id) ON DELETE SET NULL,
    tipo             varchar(20) NOT NULL
                     CONSTRAINT ck_inventario_movimientos_tipo CHECK (tipo IN ('entrada', 'salida', 'ajuste')),
    cantidad         decimal(10, 3) NOT NULL,
    stock_anterior   decimal(10, 3) NOT NULL CHECK (stock_anterior >= 0),
    stock_nuevo      decimal(10, 3) NOT NULL CHECK (stock_nuevo >= 0),
    motivo           text,
    created_at       timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_inventario_movimientos_inventario_id ON public.inventario_movimientos (inventario_id);
CREATE INDEX ix_inventario_movimientos_pedido_id ON public.inventario_movimientos (pedido_id);
CREATE INDEX ix_inventario_movimientos_compra_id ON public.inventario_movimientos (compra_id);
CREATE INDEX ix_inventario_movimientos_created_at ON public.inventario_movimientos (created_at DESC);

-- ────────────────────────────────────────────────────────────────
-- 13. NOTAS_VENTA
--     usuario_id es nullable por la misma razón que en PEDIDO_ESTADOS
--     e INVENTARIO_MOVIMIENTOS: los usuarios viven en Supabase Auth,
--     fuera del control de este script (ver seed.sql).
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.notas_venta (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    numero         varchar(20) NOT NULL UNIQUE,
    pedido_id      uuid NOT NULL UNIQUE REFERENCES public.pedidos (id) ON DELETE RESTRICT,
    usuario_id     uuid REFERENCES public.usuarios (id) ON DELETE SET NULL,
    subtotal       decimal(10, 2) NOT NULL CHECK (subtotal >= 0),
    descuento      decimal(10, 2) NOT NULL DEFAULT 0 CHECK (descuento >= 0),
    total          decimal(10, 2) NOT NULL CHECK (total >= 0),
    estado         varchar(20) NOT NULL DEFAULT 'emitida'
                   CONSTRAINT ck_notas_venta_estado CHECK (estado IN ('emitida', 'anulada')),
    observaciones  text,
    fecha_emision  timestamptz NOT NULL DEFAULT now(),
    created_at     timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_notas_venta_estado ON public.notas_venta (estado);

-- ────────────────────────────────────────────────────────────────
-- 14. AUDITORIA
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.auditoria (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id         uuid REFERENCES public.usuarios (id) ON DELETE SET NULL,
    tabla              varchar(80) NOT NULL,
    operacion          varchar(10) NOT NULL
                       CONSTRAINT ck_auditoria_operacion CHECK (operacion IN ('INSERT', 'UPDATE', 'DELETE')),
    registro_id        uuid,
    datos_anteriores   jsonb,
    datos_nuevos       jsonb,
    ip                 varchar(45),
    created_at         timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_auditoria_tabla_registro ON public.auditoria (tabla, registro_id);
CREATE INDEX ix_auditoria_created_at ON public.auditoria (created_at DESC);

-- ────────────────────────────────────────────────────────────────
-- 15. CONFIG
-- ────────────────────────────────────────────────────────────────
CREATE TABLE public.config (
    id           serial PRIMARY KEY,
    clave        varchar(100) NOT NULL UNIQUE,
    valor        text,
    descripcion  text,
    updated_at   timestamptz NOT NULL DEFAULT now()
);

-- ════════════════════════════════════════════════════════════════
-- TRIGGERS DE NEGOCIO
-- ════════════════════════════════════════════════════════════════

-- ── updated_at automático en tablas con ese campo ─────────────────
CREATE OR REPLACE FUNCTION public.fn_set_updated_at()
RETURNS trigger AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_usuarios_updated_at
  BEFORE UPDATE ON public.usuarios
  FOR EACH ROW EXECUTE FUNCTION public.fn_set_updated_at();

CREATE TRIGGER trg_clientes_updated_at
  BEFORE UPDATE ON public.clientes
  FOR EACH ROW EXECUTE FUNCTION public.fn_set_updated_at();

CREATE TRIGGER trg_productos_updated_at
  BEFORE UPDATE ON public.productos
  FOR EACH ROW EXECUTE FUNCTION public.fn_set_updated_at();

CREATE TRIGGER trg_config_updated_at
  BEFORE UPDATE ON public.config
  FOR EACH ROW EXECUTE FUNCTION public.fn_set_updated_at();

-- ── Numeración correlativa: PED-0001, NV-0001 ─────────────────────
-- Se usa una sequence dedicada por prefijo en vez de MAX(numero)+1:
-- con MAX() dos inserts concurrentes pueden leer el mismo máximo y
-- generar el mismo número (dos vendedores confirmando pedidos al
-- mismo tiempo, por ejemplo); nextval() es atómico y no tiene ese
-- problema. El único costo es que un INSERT que hace rollback deja un
-- hueco en la numeración, lo cual es aceptable y normal en un sistema
-- de correlativos.
CREATE SEQUENCE public.pedido_numero_seq;
CREATE SEQUENCE public.nota_numero_seq;

CREATE OR REPLACE FUNCTION public.fn_numero_pedido()
RETURNS varchar AS $$
BEGIN
  RETURN 'PED-' || lpad(nextval('public.pedido_numero_seq')::text, 4, '0');
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION public.fn_numero_nota()
RETURNS varchar AS $$
BEGIN
  RETURN 'NV-' || lpad(nextval('public.nota_numero_seq')::text, 4, '0');
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION public.fn_pedidos_set_numero()
RETURNS trigger AS $$
BEGIN
  IF NEW.numero IS NULL OR NEW.numero = '' THEN
    NEW.numero := public.fn_numero_pedido();
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_pedidos_set_numero
  BEFORE INSERT ON public.pedidos
  FOR EACH ROW EXECUTE FUNCTION public.fn_pedidos_set_numero();

CREATE OR REPLACE FUNCTION public.fn_notas_venta_set_numero()
RETURNS trigger AS $$
BEGIN
  IF NEW.numero IS NULL OR NEW.numero = '' THEN
    NEW.numero := public.fn_numero_nota();
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_notas_venta_set_numero
  BEFORE INSERT ON public.notas_venta
  FOR EACH ROW EXECUTE FUNCTION public.fn_notas_venta_set_numero();

-- ── Alerta automática cuando stock_actual <= stock_minimo ─────────
CREATE OR REPLACE FUNCTION public.fn_inventario_check_alerta()
RETURNS trigger AS $$
BEGIN
  NEW.alerta_activa := (NEW.stock_actual <= NEW.stock_minimo);
  NEW.updated_at := now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_inventario_check_alerta
  BEFORE INSERT OR UPDATE OF stock_actual, stock_minimo ON public.inventario
  FOR EACH ROW EXECUTE FUNCTION public.fn_inventario_check_alerta();

-- ── Descontar stock al confirmar un pedido ────────────────────────
-- Se dispara cuando PEDIDOS pasa a estado 'confirmado' (desde cualquier
-- otro estado que no sea 'confirmado'). Recorre PEDIDO_ITEMS y descuenta
-- INVENTARIO por producto_unidad_id, registrando el movimiento.
CREATE OR REPLACE FUNCTION public.fn_descontar_stock()
RETURNS trigger AS $$
DECLARE
  item record;
  inv record;
  nuevo_stock decimal(10, 3);
BEGIN
  IF NEW.estado = 'confirmado' AND OLD.estado IS DISTINCT FROM 'confirmado' THEN
    FOR item IN
      SELECT pi.producto_unidad_id, pi.cantidad
      FROM public.pedido_items pi
      WHERE pi.pedido_id = NEW.id
        AND pi.producto_unidad_id IS NOT NULL
    LOOP
      SELECT * INTO inv
      FROM public.inventario
      WHERE producto_unidad_id = item.producto_unidad_id
      FOR UPDATE;

      IF NOT FOUND THEN
        RAISE EXCEPTION 'No existe registro de inventario para producto_unidad_id %', item.producto_unidad_id;
      END IF;

      IF inv.stock_actual < item.cantidad THEN
        RAISE EXCEPTION 'Stock insuficiente para producto_unidad_id % (disponible: %, requerido: %)',
          item.producto_unidad_id, inv.stock_actual, item.cantidad;
      END IF;

      nuevo_stock := inv.stock_actual - item.cantidad;

      UPDATE public.inventario
      SET stock_actual = nuevo_stock
      WHERE id = inv.id;

      INSERT INTO public.inventario_movimientos
        (inventario_id, pedido_id, usuario_id, tipo, cantidad, stock_anterior, stock_nuevo, motivo)
      VALUES
        (inv.id, NEW.id, NEW.usuario_id, 'salida', item.cantidad, inv.stock_actual, nuevo_stock, 'pedido confirmado');
    END LOOP;
  END IF;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_descontar_stock
  AFTER UPDATE OF estado ON public.pedidos
  FOR EACH ROW EXECUTE FUNCTION public.fn_descontar_stock();

-- ── Revertir stock al cancelar un pedido ya confirmado ────────────
CREATE OR REPLACE FUNCTION public.fn_revertir_stock()
RETURNS trigger AS $$
DECLARE
  item record;
  inv record;
  nuevo_stock decimal(10, 3);
BEGIN
  IF NEW.estado = 'cancelado' AND OLD.estado IN ('confirmado', 'preparando', 'en_camino') THEN
    FOR item IN
      SELECT pi.producto_unidad_id, pi.cantidad
      FROM public.pedido_items pi
      WHERE pi.pedido_id = NEW.id
        AND pi.producto_unidad_id IS NOT NULL
    LOOP
      SELECT * INTO inv
      FROM public.inventario
      WHERE producto_unidad_id = item.producto_unidad_id
      FOR UPDATE;

      IF NOT FOUND THEN
        CONTINUE;
      END IF;

      nuevo_stock := inv.stock_actual + item.cantidad;

      UPDATE public.inventario
      SET stock_actual = nuevo_stock
      WHERE id = inv.id;

      INSERT INTO public.inventario_movimientos
        (inventario_id, pedido_id, usuario_id, tipo, cantidad, stock_anterior, stock_nuevo, motivo)
      VALUES
        (inv.id, NEW.id, NEW.usuario_id, 'ajuste', item.cantidad, inv.stock_actual, nuevo_stock, 'reversión por cancelación de pedido confirmado');
    END LOOP;
  END IF;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_revertir_stock
  AFTER UPDATE OF estado ON public.pedidos
  FOR EACH ROW EXECUTE FUNCTION public.fn_revertir_stock();

-- ── Historial automático de cambios de estado en PEDIDO_ESTADOS ───
-- Registra la transición siempre que haya usuario_id (los cambios
-- disparados por la API traen usuario_id; si no viene, no se registra
-- historial pero el cambio de estado igual ocurre).
CREATE OR REPLACE FUNCTION public.fn_registrar_pedido_estado()
RETURNS trigger AS $$
BEGIN
  IF NEW.estado IS DISTINCT FROM OLD.estado THEN
    INSERT INTO public.pedido_estados (pedido_id, usuario_id, estado_anterior, estado_nuevo)
    VALUES (NEW.id, NEW.usuario_id, OLD.estado, NEW.estado);
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_registrar_pedido_estado
  AFTER UPDATE OF estado ON public.pedidos
  FOR EACH ROW EXECUTE FUNCTION public.fn_registrar_pedido_estado();

-- ── Actualizar total_pedidos / total_gastado del cliente ──────────
CREATE OR REPLACE FUNCTION public.fn_actualizar_totales_cliente()
RETURNS trigger AS $$
BEGIN
  IF NEW.estado = 'entregado' AND OLD.estado IS DISTINCT FROM 'entregado' THEN
    UPDATE public.clientes
    SET total_pedidos = total_pedidos + 1,
        total_gastado = total_gastado + NEW.total,
        updated_at = now()
    WHERE id = NEW.cliente_id;
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_actualizar_totales_cliente
  AFTER UPDATE OF estado ON public.pedidos
  FOR EACH ROW EXECUTE FUNCTION public.fn_actualizar_totales_cliente();

-- ════════════════════════════════════════════════════════════════
-- ROW LEVEL SECURITY
-- ════════════════════════════════════════════════════════════════
-- La API .NET siempre accede con la Service Role Key (bypassa RLS por
-- diseño de Supabase), así que estas políticas solo importan si en el
-- futuro algo se consulta con el AnonKey o un JWT de "authenticated"
-- directamente contra Supabase (p. ej. desde el frontend).

ALTER TABLE public.pedidos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.inventario ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.inventario_movimientos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.usuarios ENABLE ROW LEVEL SECURITY;

-- Resto de tablas del schema: sin política para anon/authenticated (nadie
-- las consulta con esas credenciales hoy; el storefront y el admin pasan
-- siempre por la API .NET con la Service Role Key). Alcanza con habilitar
-- RLS y no definir políticas: eso deniega por defecto a anon/authenticated
-- y la service role sigue teniendo acceso total porque bypassa RLS.
ALTER TABLE public.clientes ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.categorias ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.productos ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.producto_unidades ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.pedido_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.pedido_item_precio_historial ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.pedido_estados ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.pedido_pago_historial ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.notas_venta ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.auditoria ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.config ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.compras ENABLE ROW LEVEL SECURITY;

-- PEDIDOS: cualquier usuario interno autenticado puede leer todos los
-- pedidos (son empleados de la misma empresa); solo la service role
-- puede insertar/actualizar (lo hace la API).
CREATE POLICY pedidos_select_authenticated ON public.pedidos
  FOR SELECT
  TO authenticated
  USING (true);

CREATE POLICY pedidos_all_service_role ON public.pedidos
  FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

-- INVENTARIO / INVENTARIO_MOVIMIENTOS: solo la API (service role).
CREATE POLICY inventario_all_service_role ON public.inventario
  FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

CREATE POLICY inventario_movimientos_all_service_role ON public.inventario_movimientos
  FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

-- COMPRAS: solo la API (service role) — mismo criterio que INVENTARIO.
CREATE POLICY compras_all_service_role ON public.compras
  FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

-- USUARIOS: un usuario autenticado puede leer su propia fila; la API
-- (service role) puede todo.
CREATE POLICY usuarios_select_self ON public.usuarios
  FOR SELECT
  TO authenticated
  USING (id = auth.uid());

CREATE POLICY usuarios_all_service_role ON public.usuarios
  FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

-- STORAGE: bucket "productos" para las imágenes de producto que se suben
-- desde el panel admin (reemplaza el flujo anterior de pegar una URL a
-- mano). Público de lectura (así imagenUrl sirve como URL directa para el
-- storefront); solo un usuario autenticado (staff con cuenta en el panel)
-- puede subir/reemplazar/borrar objetos de este bucket.
INSERT INTO storage.buckets (id, name, public)
VALUES ('productos', 'productos', true)
ON CONFLICT (id) DO NOTHING;

CREATE POLICY productos_storage_insert_authenticated ON storage.objects
  FOR INSERT
  TO authenticated
  WITH CHECK (bucket_id = 'productos');

CREATE POLICY productos_storage_update_authenticated ON storage.objects
  FOR UPDATE
  TO authenticated
  USING (bucket_id = 'productos')
  WITH CHECK (bucket_id = 'productos');

CREATE POLICY productos_storage_delete_authenticated ON storage.objects
  FOR DELETE
  TO authenticated
  USING (bucket_id = 'productos');
