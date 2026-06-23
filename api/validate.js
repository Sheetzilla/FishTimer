import { createClient } from '@supabase/supabase-js';

const supabase = createClient(
  process.env.SUPABASE_URL,
  process.env.SUPABASE_SERVICE_KEY
);

export default async function handler(req, res) {
  const key = req.query.key;

  if (!key) return res.status(400).send('No key provided');

  const { data } = await supabase
    .from('licenses')
    .select('key')
    .eq('key', key)
    .single();

  if (data) {
    return res.status(200).send('valid');
  } else {
    return res.status(200).send('invalid');
  }
}