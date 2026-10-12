-- 29 Eyl 2026'da kaldırılan Supabase-native kalıntılar.
-- Hepsi EF Core öncesi snake_case şemaya (public.profiles, public.events,
-- public.event_participants) referans veriyordu; o tablolar artık yok, yani
-- çalıştırılsalar hata verirlerdi. auth.users boş olduğu için
-- on_auth_user_created trigger'ı da hiç tetiklenmiyordu.
-- Yalnızca geri alma ihtiyacı doğarsa referans olsun diye saklanıyor.

CREATE OR REPLACE FUNCTION public.handle_new_user()
 RETURNS trigger
 LANGUAGE plpgsql
 SECURITY DEFINER
AS $function$
BEGIN
  INSERT INTO public.profiles (id, full_name, avatar_url)
  VALUES (
    new.id,
    COALESCE(new.raw_user_meta_data->>'full_name', 'Yeni Kullanıcı'),
    new.raw_user_meta_data->>'avatar_url'
  );
  RETURN new;
END;
$function$;

CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

CREATE OR REPLACE FUNCTION public.auto_join_event_creator()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
  INSERT INTO public.event_participants (event_id, user_id, status)
  VALUES (NEW.id, NEW.created_by, 'approved');
  RETURN NEW;
END;
$function$;

CREATE OR REPLACE FUNCTION public.update_event_participants_count()
 RETURNS trigger
 LANGUAGE plpgsql
 SECURITY DEFINER
AS $function$
BEGIN
  IF (TG_OP = 'INSERT') THEN
    UPDATE public.events
    SET participants_count = participants_count + 1
    WHERE id = NEW.event_id;
  ELSIF (TG_OP = 'DELETE') THEN
    UPDATE public.events
    SET participants_count = GREATEST(0, participants_count - 1)
    WHERE id = OLD.event_id;
  END IF;
  RETURN NULL;
END;
$function$;

CREATE OR REPLACE FUNCTION public.get_nearby_events(user_lat double precision, user_lon double precision)
 RETURNS TABLE(id uuid, title text, sport_type text, event_date timestamp with time zone, address_text text, latitude double precision, longitude double precision, distance_km double precision)
 LANGUAGE sql
AS $function$
  SELECT
    id, title, sport_type, event_date, address_text, latitude, longitude,
    (6371 * acos(
      cos(radians(user_lat)) * cos(radians(latitude)) *
      cos(radians(longitude) - radians(user_lon)) +
      sin(radians(user_lat)) * sin(radians(latitude))
    )) AS distance_km
  FROM public.events
  ORDER BY distance_km ASC;
$function$;
