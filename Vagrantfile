 

# -*- mode: ruby -*- 

# vi: set ft=ruby : 

 

Vagrant.configure("2") do |config| 

  config.ssh.insert_key = false 
 

  config.vm.box = "ubuntu/jammy64" 
  
  config.vm.network "forwarded_port", guest: 50555, host: 50555
  config.vm.network "forwarded_port", guest: 55055, host: 55055
  config.vm.network "forwarded_port", guest: 2375, host: 2375
 

  config.vm.provider "virtualbox" do |v| 

    v.gui = false 

    v.memory = 2048 

    v.cpus = 1 

  end 

 

  # Docker Machine 

  config.vm.define "docker" do |docker| 

    docker.vm.hostname = "docker" 

    docker.vm.network "private_network", ip: "192.168.99.100" 

    docker.vm.provision "shell", path: "add_hosts.sh" 

    docker.vm.provision "shell", path: "docker-setup.sh" 
	
	config.vm.synced_folder "./", "/vagrant_data"

  end 
 

end
