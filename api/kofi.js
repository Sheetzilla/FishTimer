import { createClient } from '@supabase/supabase-js';

const supabase = createClient(
  process.env.SUPABASE_URL,
  process.env.SUPABASE_SERVICE_KEY
);

export default async function handler(req, res) {
  if (req.method !== 'POST') return res.status(405).send('Method not allowed');

  // Ko-fi wraps everything in a "data" field
  const data = JSON.parse(req.body.data);

  // Block fake requests
  if (data.verification_token !== process.env.KOFI_TOKEN) {
    return res.status(401).send('Unauthorized');
  }

  // Generate a key like TIMER-A3F9-X2K1
  const key = 'TIMER-' + crypto.randomUUID().split('-').slice(0, 2).join('-').toUpperCase();

  // Save to Supabase (which will auto-email the buyer)
  await supabase.from('licenses').insert({
    email: data.email,
    key: key,
    used: false,
  });

  return res.status(200).send('OK');
}