import { createClient } from '@supabase/supabase-js';

const supabase = createClient(
  process.env.SUPABASE_URL,
  process.env.SUPABASE_SERVICE_KEY
);

export default async function handler(req, res) {
  const { key, device } = req.query;

  if (!key || !device) {
    return res.status(400).send('missing');
  }

  const { data: license, error } = await supabase
    .from('licenses')
    .select('*')
    .eq('key', key)
    .single();

  if (error || !license) {
    return res.status(200).send('invalid');
  }

  const devices = license.device_ids || [];

  // Device already activated
  if (devices.includes(device)) {
    return res.status(200).send('valid');
  }

  // Activation limit reached
  if (license.activations >= license.max_activations) {
    return res.status(200).send('invalid');
  }

  // Register new device
  const { error: updateError } = await supabase
    .from('licenses')
    .update({
      activations: license.activations + 1,
      device_ids: [...devices, device]
    })
    .eq('id', license.id);

  if (updateError) {
    console.error(updateError);
    return res.status(500).send('error');
  }

  return res.status(200).send('valid');
}