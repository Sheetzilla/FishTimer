import { createClient } from '@supabase/supabase-js';

const supabase = createClient(
  process.env.SUPABASE_URL,
  process.env.SUPABASE_SERVICE_KEY
);

export default async function handler(req, res) {
  if (req.method !== 'POST') return res.status(405).send('Method not allowed');

  try {
    const data = JSON.parse(req.body.data);

    if (data.verification_token !== process.env.KOFI_TOKEN) {
      return res.status(401).send('Unauthorized');
    }

    const key = 'TIMER-' + crypto.randomUUID().split('-').slice(0, 2).join('-').toUpperCase();

    console.log('Attempting insert with email:', data.email);
    console.log('SUPABASE_URL:', process.env.SUPABASE_URL);

    const { data: insertData, error } = await supabase.from('licenses').insert({
      email: data.email,
      key: key,
      activations: 0,
      max_activations: 3,
      device_ids: [],
    });

    if (error) {
      console.error('Supabase insert error:', JSON.stringify(error));
      return res.status(500).send('DB error');
    }

    console.log('Insert successful:', insertData);
    return res.status(200).send('OK');

  } catch (err) {
    console.error('Caught error:', err.message);
    return res.status(500).send('Server error');
  }
}